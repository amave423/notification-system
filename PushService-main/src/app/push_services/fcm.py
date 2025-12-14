from app.notificationSender import NotificationSender, NotificationError
from app.models import Message
from firebase_admin import credentials, messaging
import firebase_admin
import os
import logging
from dotenv import load_dotenv

load_dotenv()

logger = logging.getLogger(__name__)

class FCM(NotificationSender):
    """Firebase Cloud Messaging."""
    cred_path = os.getenv("CRED_PATH", None)
    _initialized = False

    @classmethod
    def initialize(cls):
        """Инициализация Firebase."""
        if not cls._initialized:
            try:
                cred = credentials.Certificate(cls.cred_path)
                firebase_admin.initialize_app(cred)
                logger.info(msg = "Firebase initialized successfully.")
                cls._initialized = True
            except FileNotFoundError:
                logger.info(msg = f"Error: Credentials file not found at {cls.cred_path}")
                return False
            except Exception as e:
                logger.info(msg = f"Error initializing Firebase: {e}")
                return False
        return True  

    @classmethod
    def send(cls, message: Message):
        """Отправка сообщения."""
        cls.initialize()
        if not cls._initialized:
            raise NotificationError

        message = messaging.Message(
            notification=messaging.Notification(
                title = message.title,
                body = message.body
            ),
        )

        try:
            response = messaging.send(message)
            logger.info(msg = f"Successfully sent message: {response}")
            return True
        except Exception as e:
            logger.info(msg = f"Error sending message: {e}")
            return False