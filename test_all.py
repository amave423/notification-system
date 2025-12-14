import requests
import time
import sys

def check_service(name, url, auth=None, timeout=5):
    """Проверяет доступность сервиса"""
    try:
        if auth:
            response = requests.get(url, auth=auth, timeout=timeout)
        else:
            response = requests.get(url, timeout=timeout)
        
        if response.status_code < 500:
            return True, f" {name}: работает (HTTP {response.status_code})"
        else:
            return False, f"  {name}: ошибка HTTP {response.status_code}"
    except requests.exceptions.ConnectionError:
        return False, f" {name}: не доступен"
    except Exception as e:
        return False, f" {name}: ошибка - {str(e)[:50]}..."

def test_all_services():
    print("="*70)
    print(" ПОЛНЫЙ ТЕСТ СИСТЕМЫ УВЕДОМЛЕНИЙ")
    print("="*70)
    
    services_to_check = [
        ("RabbitMQ", "http://localhost:15672/api/overview", ('guest', 'guest')),
        ("PostgreSQL (шлюз)", "http://localhost:8000/health", None),
        ("PostgreSQL (email)", None, None),
        ("Шлюз уведомлений", "http://localhost:8000/health", None),
        ("Email Service", "http://localhost:5130", None),
        ("SMS Service", "http://localhost:5004/health", None),
        ("Push Service", "http://localhost:8001/health", None),
        ("MailHog", "http://localhost:8025", None),
    ]
    
    all_ok = True
    for name, url, auth in services_to_check:
        if url:
            ok, message = check_service(name, url, auth)
            print(message)
            if not ok:
                all_ok = False
        else:
            print(f"🔍 {name}: проверка через сервисы...")
    
    if not all_ok:
        print("\n  Некоторые сервисы не доступны. Проверьте логи:")
        print("  docker-compose logs --tail=100")
        return False
    
    print("\n" + "="*70)
    print("🧪 ТЕСТИРУЕМ ОТПРАВКУ УВЕДОМЛЕНИЙ")
    print("="*70)
    
    # Тест Email через шлюз
    print("\n1.  Тест Email уведомления через шлюз:")
    try:
        payload = {
            "type": "email",
            "recipient": "test@example.com",
            "subject": "Тест из Python-скрипта",
            "message": "<h1>Привет!</h1><p>Это тестовое email сообщение</p>"
        }
        response = requests.post(
            "http://localhost:8000/api/v1/notifications",
            json=payload,
            timeout=10
        )
        
        if response.status_code == 202:
            data = response.json()
            print(f" Успешно отправлено!")
            print(f"   ID уведомления: {data['id']}")
            print(f"   Тип: {data['type']}")
            print(f"   Статус: {data['status']}")
            print(f"   Получатель: {data['recipient']}")
            
            time.sleep(2)
            status_response = requests.get(
                f"http://localhost:8000/api/v1/notifications/{data['id']}"
            )
            if status_response.status_code == 200:
                status_data = status_response.json()
                print(f"   Текущий статус: {status_data['status']}")
        else:
            print(f" Ошибка: HTTP {response.status_code}")
            print(f"   Ответ: {response.text[:200]}")
            
    except Exception as e:
        print(f" Исключение: {e}")
    
    # Тест SMS через шлюз
    print("\n2. 📱 Тест SMS уведомления через шлюз:")
    try:
        payload = {
            "type": "sms",
            "recipient": "+79123456789",
            "message": "Тестовое SMS сообщение из Python"
        }
        response = requests.post(
            "http://localhost:8000/api/v1/notifications",
            json=payload,
            timeout=10
        )
        
        if response.status_code == 202:
            data = response.json()
            print(f" Успешно отправлено!")
            print(f"   ID уведомления: {data['id']}")
            print(f"   Тип: {data['type']}")
            print(f"   Статус: {data['status']}")
        else:
            print(f" Ошибка: HTTP {response.status_code}")
            print(f"   Ответ: {response.text[:200]}")
            
    except Exception as e:
        print(f" Исключение: {e}")
    
    # Прямой тест SMS сервиса
    print("\n3.  Прямой тест SMS сервиса:")
    try:
        payload = {
            "PhoneNumber": "+79123456789",
            "Message": "Прямое SMS сообщение",
            "Sender": "TEST"
        }
        response = requests.post(
            "http://localhost:5004/api/sms/send-sync",
            json=payload,
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            print(f" Успешно отправлено!")
            print(f"   Message ID: {data.get('messageId', 'N/A')}")
            print(f"   Статус: {data.get('status', 'N/A')}")
            print(f"   Время отправки: {data.get('sentAt', 'N/A')}")
        else:
            print(f" Ошибка: HTTP {response.status_code}")
            print(f"   Ответ: {response.text[:200]}")
            
    except Exception as e:
        print(f" Исключение: {e}")
    
    # Тест Push сервиса
    print("\n4.  Тест Push сервиса:")
    try:
        response = requests.get("http://localhost:8001/", timeout=5)
        if response.status_code == 200:
            print(f" Push сервис работает: {response.json()}")
        else:
            print(f"  Push сервис: HTTP {response.status_code}")
            
    except Exception as e:
        print(f" Push сервис не доступен: {e}")
    
    # Проверка очередей RabbitMQ
    print("\n5.  Проверка RabbitMQ очередей:")
    try:
        response = requests.get(
            "http://localhost:15672/api/queues",
            auth=('guest', 'guest'),
            timeout=5
        )
        if response.status_code == 200:
            queues = response.json()
            print(f" Найдено очередей: {len(queues)}")
            for queue in queues:
                print(f" {queue['name']}: {queue['messages']} сообщений")
        else:
            print(f" Не удалось получить список очередей")
            
    except Exception as e:
        print(f" Ошибка RabbitMQ: {e}")
    
    
    print("\n ВСЕ СЕРВИСЫ ДОСТУПНЫ ПО АДРЕСАМ:")
    print(" 1.  Шлюз уведомлений (основной сервис)")
    print("     API: http://localhost:8000/docs")
    print("     Health: http://localhost:8000/health")
    print(" 2.  Email Service")
    print("     Метрики: http://localhost:5130/metrics")
    print("     Порт: 5130")
    print(" 3.  SMS Service")
    print("     Swagger UI: http://localhost:5004/swagger")
    print("     Health: http://localhost:5004/health")
    print(" 4.  Push Service")
    print("     API: http://localhost:8001/docs")
    print("     Health: http://localhost:8001/health")
    print(" 5.  RabbitMQ Management")
    print("     Веб-интерфейс: http://localhost:15672")
    print("     Логин: guest / guest")
    print(" 6.  MailHog (тестовый SMTP сервер)")
    print("     Веб-интерфейс: http://localhost:8025")
    print("     SMTP порт: localhost:1025")

    
    return True

if __name__ == "__main__":
    print(" Запускаем проверку системы...")
    time.sleep(3)  # Даем сервисам время на запуск
    success = test_all_services()
    sys.exit(0 if success else 1)