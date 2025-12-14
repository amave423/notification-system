import aio_pika
import json
import logging
import asyncio
from app.notificationSender import NotificationSender
from app.rabbitmq.consumer_work import send_push

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class RabbitMQConsumer:
    def __init__(self, rabbitmq_url, queue_name):
        self.rabbitmq_url = rabbitmq_url
        self.queue_name = queue_name
        self.connection = None
        self.channel = None

    async def connect(self):
        """Устанавливает асинхронное соединение с RabbitMQ."""
        self.connection = await aio_pika.connect_robust(self.rabbitmq_url)
        self.channel = await self.connection.channel()
        await self.channel.set_qos(prefetch_count=1) 
        self.queue = await self.channel.declare_queue(name=self.queue_name, durable=True)

    async def callback(self, message: aio_pika.IncomingMessage):
        """Обрабатывает полученные сообщения."""
        async with message.process():
            body = message.body
            message_data = json.loads(body)
            logger.info(f"Received message: {message_data}")
            await send_push(message = message_data)

    async def start_consuming(self):
        """Запускает процесс прослушивания очереди."""
        await self.connect() 
        logger.info("Consuming...")
        
        await self.queue.consume(self.callback)

    async def close_connection(self):
        """Закрывает соединение с RabbitMQ."""
        if self.connection:
            await self.connection.close()