# Notification System

Это монолитный набор микросервисов для отправки уведомлений (gateway + workers): Email Service, SMS Service, Push Service, RabbitMQ, PostgreSQL, Elasticsearch и тестовые утилиты (MailHog и скрипт тестирования).

Сервисы и порты
---------------
- Notification Gateway (FastAPI)
  - API: http://localhost:8000/docs
  - Health: http://localhost:8000/health
- Email Service (ASP.NET)
  - Порт: 5130 (в контейнере 8080)
  - Метрики: http://localhost:5130/metrics
- SMS Service (.NET)
  - Swagger: http://localhost:5004/swagger
  - Health: http://localhost:5004/health
  - API: POST http://localhost:5004/api/sms/send-sync
- Push Service (FastAPI)
  - API / health: http://localhost:8001/health
- RabbitMQ Management
  - UI: http://localhost:15672 (guest / guest)
- MailHog (тестовый SMTP)
  - UI: http://localhost:8025
  - SMTP: localhost:1025
- Elasticsearch: http://localhost:9200
- PostgreSQL:
  - Gateway DB: localhost:5432
  - Email DB: localhost:5433 (внутренний 5432)

Быстрый старт (локально)
------------------------
1. Из корня проекта запустить все сервисы:
   docker-compose up --build -d
2. Подождать, пока контейнеры станут healthy (можно смотреть логи):
   docker-compose logs --tail=100 -f
3. Открыть Swagger / UIs:
   - Gateway: http://localhost:8000/docs
   - RabbitMQ: http://localhost:15672
   - MailHog: http://localhost:8025

Тестирование (локально)
-----------------------
В корне проекта есть скрипт тестирования: `test_all.py`. Он:
- Проверяет доступность основных HTTP/health-эндпоинтов.
- Пытается отправить тестовые уведомления через gateway и напрямую в SMS сервис.
- Читает очереди RabbitMQ через management API.

Запуск:
python test_all.py

Структура проекта
-------------------------
- notification-main/ — шлюз (FastAPI), принимает уведомления и кладёт в RabbitMQ.
- email-notification-service-main/ — Email сервис (ASP.NET), читает очередь, отправляет email через MailKit/MailHog.
- SmsSenderService-master/ — SMS сервис (.NET), API для синхронной и асинхронной отправки (MassTransit/RabbitMQ).
- PushService-main/ — Push сервис (FastAPI/Python), отправляет push-уведомления (Pushbullet / FCM).
- docker-compose.yml — корневой compose (составляет инфраструктуру).
- test_all.py — интеграционный/проверочный скрипт в корне.

Советы по отладке
-----------------
- Если сервисы не стартуют, проверьте healthchecks контейнеров: docker-compose ps
- Для БД: убедитесь, что порты 5432/5433 не заняты локальными инстансами PostgreSQL
- Для .NET-сервисов — смотреть вывод контейнера (dotnet logging / Serilog)
- Для email-тестов используйте MailHog UI (8025) — все отправленные письма видны там

Где искать детали
-----------------
Подробные README и реализации лежат в подпапках:
- notification-main/README.md — шлюз (FastAPI)
- email-notification-service-main/README.md — Email Service (C#)
- SmsSenderService-master/README.md — SMS Service (C#)
- PushService-main/README.md — Push Service (Python)

Кто делал
-------------------
- email-notification-service-main - Козлов Данил
- notification-main - Гребенщиков Данил
- PushService-main - Казанцев Семён
- SmsSenderService-master - Антропов Артём
