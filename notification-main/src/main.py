import sys
import os
from fastapi import FastAPI, Depends, HTTPException, status
from sqlalchemy.orm import Session
from sqlalchemy import create_engine, Column, String, Text, Integer, DateTime
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.sql import func
import uuid
import json
import pika
import logging
import time

# Настройка логирования
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# База данных - добавляем задержку для инициализации
DATABASE_URL = "postgresql://notification_user:password@postgres-gateway:5432/notifications"

def create_db_engine():
    """Создает engine с повторными попытками подключения"""
    max_retries = 5
    for attempt in range(max_retries):
        try:
            engine = create_engine(DATABASE_URL)
            # Проверяем подключение
            with engine.connect() as conn:
                pass
            logger.info("✅ Successfully connected to PostgreSQL")
            return engine
        except Exception as e:
            if attempt < max_retries - 1:
                wait_time = 2 ** attempt
                logger.warning(f"⚠️ Failed to connect to PostgreSQL (attempt {attempt + 1}/{max_retries}). Retrying in {wait_time}s...")
                time.sleep(wait_time)
            else:
                logger.error(f"❌ Failed to connect to PostgreSQL after {max_retries} attempts: {e}")
                raise

try:
    engine = create_db_engine()
    SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
    Base = declarative_base()
except Exception as e:
    logger.error(f"❌ Database initialization failed: {e}")
    logger.info("💡 Make sure Docker containers are running: docker-compose up -d")
    sys.exit(1)

def generate_uuid():
    return str(uuid.uuid4())

class Notification(Base):
    __tablename__ = "notifications"
    id = Column(String(36), primary_key=True, default=generate_uuid)
    type = Column(String(50), nullable=False)
    recipient = Column(String(500), nullable=False)
    subject = Column(String(1000))
    message = Column(Text, nullable=False)
    status = Column(String(20), default='pending')
    retry_count = Column(Integer, default=0)
    max_retries = Column(Integer, default=3)
    created_at = Column(DateTime, default=func.now())
    updated_at = Column(DateTime, default=func.now(), onupdate=func.now())

# Создаем таблицы
try:
    Base.metadata.create_all(bind=engine)
    logger.info("✅ Database tables created successfully")
except Exception as e:
    logger.error(f"❌ Failed to create database tables: {e}")

# FastAPI приложение
app = FastAPI(
    title="Notification Gateway",
    description="Микросервис для отправки уведомлений",
    version="1.0.0"
)

# Зависимость для БД
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# Простой шлюз с обработкой ошибок
class NotificationGateway:
    def __init__(self, rabbitmq_url: str):
        self.rabbitmq_url = rabbitmq_url
        
    def route_notification(self, notification, db):
        try:
            connection = pika.BlockingConnection(pika.URLParameters(self.rabbitmq_url))
            channel = connection.channel()
            
            # Объявляем очередь
            queue_name = f"notification_{notification.type}"
            channel.queue_declare(queue=queue_name, durable=True)
            
            # Подготавливаем данные
            message_data = {
                "id": str(notification.id),
                "type": notification.type,
                "recipient": notification.recipient,
                "subject": notification.subject,
                "message": notification.message
            }
            
            # Отправляем в очередь
            channel.basic_publish(
                exchange='',
                routing_key=queue_name,
                body=json.dumps(message_data),
                properties=pika.BasicProperties(delivery_mode=2)
            )
            
            connection.close()
            logger.info(f"✅ Notification {notification.id} routed to {queue_name}")
            
        except Exception as e:
            logger.error(f"❌ Failed to route notification to RabbitMQ: {e}")
            # Можно добавить логику повторных попыток или сохранения в БД

gateway = NotificationGateway("amqp://guest:guest@rabbitmq:5672/")

@app.post("/api/v1/notifications", status_code=status.HTTP_202_ACCEPTED)
async def create_notification(notification_data: dict, db: Session = Depends(get_db)):
    """
    Создание уведомления
    """
    try:
        # Валидация обязательных полей
        required_fields = ['type', 'recipient', 'message']
        for field in required_fields:
            if field not in notification_data:
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST,
                    detail=f"Missing required field: {field}"
                )
        
        # Проверка типа уведомления
        supported_types = ['email', 'sms', 'push', 'telegram', 'whatsapp']
        if notification_data['type'] not in supported_types:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Unsupported notification type. Supported: {supported_types}"
            )
        
        # Создаем уведомление
        db_notification = Notification(
            type=notification_data['type'],
            recipient=notification_data['recipient'],
            subject=notification_data.get('subject', ''),
            message=notification_data['message']
        )
        
        db.add(db_notification)
        db.commit()
        db.refresh(db_notification)
        
        # Маршрутизируем
        gateway.route_notification(db_notification, db)
        
        return {
            "id": db_notification.id,
            "type": db_notification.type,
            "recipient": db_notification.recipient,
            "status": db_notification.status,
            "created_at": db_notification.created_at.isoformat()
        }
        
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to process notification: {str(e)}"
        )

@app.get("/api/v1/notifications/{notification_id}")
async def get_notification_status(notification_id: str, db: Session = Depends(get_db)):
    """
    Получение статуса уведомления
    """
    try:
        notification = db.query(Notification).filter(Notification.id == notification_id).first()
        
        if not notification:
            raise HTTPException(status_code=404, detail="Notification not found")
        
        return {
            "id": notification.id,
            "status": notification.status,
            "retry_count": notification.retry_count,
            "created_at": notification.created_at.isoformat()
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/health")
async def health_check():
    """
    Проверка здоровья сервиса
    """
    try:
        # Проверяем подключение к БД
        with engine.connect() as conn:
            pass
        db_status = "healthy"
    except:
        db_status = "unhealthy"
    
    return {
        "status": "healthy" if db_status == "healthy" else "degraded",
        "service": "notification-gateway",
        "database": db_status,
        "timestamp": time.time()
    }

@app.get("/")
async def root():
    """
    Корневой endpoint
    """
    return {
        "message": "Notification Gateway API",
        "version": "1.0.0",
        "endpoints": {
            "send_notification": "POST /api/v1/notifications",
            "check_status": "GET /api/v1/notifications/{id}",
            "health": "GET /health"
        }
    }

if __name__ == "__main__":
    import uvicorn
    print("🚀 Starting Notification Gateway...")
    print("🐘 PostgreSQL: localhost:5432")
    print("🐇 RabbitMQ: localhost:5672")
    print("📧 Supported notification types: email, sms, push, telegram, whatsapp")
    print("🔗 Health check: http://localhost:8000/health")
    print("📚 API docs: http://localhost:8000/docs")
    print("=" * 50)
    uvicorn.run(app, host="0.0.0.0", port=8000, log_level="info")