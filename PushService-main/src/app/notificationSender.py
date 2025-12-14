from abc import ABC, abstractmethod
from app.models import Message

class NotificationError(Exception):
    pass

class NotificationSender(ABC):
    @classmethod
    @abstractmethod    
    async def async_send(message: Message):
        ...