from app.notificationSender import NotificationSender, NotificationError
from app.models import NotificationRequest
import logging
from app.push_services.fcm import FCM
from app.push_services.pushbullet import PushBulletSender

logger = logging.getLogger(__name__)

push_services: dict[str: NotificationSender] = {
    "fcm": FCM,
    "pushbullet": PushBulletSender
}

async def send_push(message: NotificationRequest) -> None:
    service: NotificationSender = push_services.get(message.channel_type, None)
    if service:
        logger.info(msg = 'push service choosed')
        try:
            await service.async_send(message.message)
            logger.info(msg = 'Send succesfuly!')
        except NotificationError:
            logger.info(msg = 'Failed to sent message')
    logger.info(msg = 'No such service')