import pytest
from pytest_mock import MockerFixture
from app.push_services.fcm import FCM
from app.notificationSender import NotificationError

def test_initialize_FCM_correct(mocker: MockerFixture):
    mocker.patch('app.push_services.fcm.FCM._initialized', new = True)
    assert FCM.initialize() == True

def test_initialize_FCM_incorrect_cred(mocker: MockerFixture):
    mocker.patch('app.push_services.fcm.firebase_admin.initialize_app', side_effect = FileNotFoundError)
    assert FCM.initialize() == False

def test_initialize_FCM_error(mocker: MockerFixture):
    mocker.patch('app.push_services.fcm.firebase_admin.initialize_app', side_effect = Exception)
    assert FCM.initialize() == False

def test_send_message_FCM_error_initialize(mocker: MockerFixture, message):
    mocker.patch('app.push_services.fcm.FCM.initialize', return_value = False)
    with pytest.raises(NotificationError):
        FCM.send(message = message)

def test_send_message_correct(mocker: MockerFixture, message):
    mocker.patch('app.push_services.fcm.FCM._initialized', new = True)
    mocker.patch('app.push_services.fcm.messaging.send', return_value = True)
    
    assert FCM.send(message = message) == True
    