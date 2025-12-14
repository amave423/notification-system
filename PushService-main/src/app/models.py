from pydantic import BaseModel

class Message(BaseModel):
    title: str
    body: str

class NotificationRequest(BaseModel):
    channel_type: str
    message: Message