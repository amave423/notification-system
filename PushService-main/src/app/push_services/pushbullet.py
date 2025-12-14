import os
from app.models import Message
from asyncpushbullet import AsyncPushbullet
from pushbullet.errors import InvalidKeyError, PushError
from app.notificationSender import NotificationSender, NotificationError

class PushBulletSender(NotificationSender):
    """PushBullet."""
    @classmethod
    async def async_send(cls, message: Message):
        """Отправка сообщения."""
        api_key = os.getenv('PUSHBULLET_API_KEY', None)
        try:
            pb = AsyncPushbullet(api_key)
        except InvalidKeyError as publicKeyError:
            raise NotificationError from publicKeyError
        try:
            push = await pb.async_push_note(title = message.title, body = message.body)
        except PushError as pushError:
            raise NotificationError from pushError