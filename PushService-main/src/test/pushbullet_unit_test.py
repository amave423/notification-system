import pytest
from pytest_mock import MockerFixture
from unittest.mock import AsyncMock
from app.notificationSender import NotificationError
from app.push_services.pushbullet import PushBulletSender
from pushbullet.errors import PushError
from asyncpushbullet.errors import InvalidKeyError
import pytest_asyncio

@pytest.mark.asyncio
async def test_pushbullet_send_correct(message, mocker: MockerFixture):
    pushbullet_mock = AsyncMock()
    mocker.patch(
    'app.push_services.pushbullet.AsyncPushbullet',
    return_value = pushbullet_mock)
    pushbullet_mock.async_push_note.return_value = True
    response = await PushBulletSender.async_send(message = message)
    assert response == None

@pytest.mark.asyncio
async def test_pushbullet_send_wrong_api_key(message, monkeypatch):
    monkeypatch.setenv("PUSHBULLET_API_KEY", "INVALID_KEY")
    with pytest.raises(InvalidKeyError) as test_error:
        await PushBulletSender.async_send(message = message)

@pytest.mark.asyncio
async def test_pushbullet_send_push_error(message, mocker: MockerFixture):
    pushbullet_mock = AsyncMock()
    mocker.patch(
    'app.push_services.pushbullet.AsyncPushbullet',
    return_value = pushbullet_mock)
    pushbullet_mock.async_push_note.side_effect = PushError
    with pytest.raises(NotificationError) as test_error:
        await PushBulletSender.async_send(message = message)
    assert isinstance(test_error.value.__cause__, PushError)

