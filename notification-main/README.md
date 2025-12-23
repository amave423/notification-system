Notification Gateway

    Краткое описание
Notification Gateway — это FastAPI-сервис, который принимает запросы на отправку уведомлений через HTTP API и маршрутизирует их через RabbitMQ в соответствующие очереди для дальнейшей обработки.


    Основная функция
Сервис выступает в качестве шлюза (gateway) между клиентами и системой отправки уведомлений:

Принимает HTTP-запросы на отправку уведомлений

Сохраняет информацию в PostgreSQL

Отправляет задачи в RabbitMQ очереди

Предоставляет API для проверки статуса уведомлений



    Что делает этот код
Основные компоненты:
FastAPI приложение - REST API для работы с уведомлениями

PostgreSQL - хранение статусов и метаданных уведомлений

RabbitMQ - очередь сообщений для распределения задач

SQLAlchemy - ORM для работы с базой данных

    Ключевые возможности:
✅ Создание уведомлений через POST /api/v1/notifications

✅ Проверка статуса через GET /api/v1/notifications/{id}

✅ Поддержка нескольких типов уведомлений: email, sms, push, telegram, whatsapp

✅ Повторные попытки подключения к БД при старте

✅ Health-check эндпоинт (/health)

✅ Автоматическое создание таблиц в БД


    Как запустить
Быстрый старт:
bash
# Запуск всех сервисов
docker-compose up -d

# Запуск только сервиса (если БД и RabbitMQ уже запущены)
python start_simple.py
После запуска:

API доступен на http://localhost:8000

Документация Swagger: http://localhost:8000/docs

RabbitMQ Management: http://localhost:15672 (guest/guest)


    Пример использования
bash
# Отправить email уведомление
curl -X POST http://localhost:8000/api/v1/notifications \
  -H "Content-Type: application/json" \
  -d '{
    "type": "email",
    "recipient": "user@example.com",
    "subject": "Приветствие",
    "message": "Добро пожаловать!"
  }'

    Архитектура
text
Клиент → HTTP API → PostgreSQL (сохранение) → RabbitMQ (очередь) → Воркеры
              ↑
GET статуса ←─┘
Этот сервис является только entry-point'ом — он не отправляет уведомления напрямую, а только ставит их в очередь для обработки специализированными воркерами.
