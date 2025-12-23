# SMS SENDER SERVICE

# 🚀 БЫСТРЫЙ СТАРТ
docker-compose up --build -d

# 📡 API
curl -X POST "http://localhost:5004/api/sms/send-sync" \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+79161234567","message":"Test"}'

curl -X POST "http://localhost:5004/api/sms/send-async" \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+79161234567","message":"Test async"}'

# 📊 МОНИТОРИНГ
# RabbitMQ UI: http://localhost:15672 (guest/guest)

# 🐳 DOCKER
docker-compose up -d      # запуск
docker-compose down       # остановка
docker-compose logs -f    # логи

# 📁 СТРУКТУРА
SmsSenderService/
├── Program.cs            # точка входа
├── appsettings.json      # конфиг
├── Dockerfile           # docker
├── docker-compose.yml   # compose
├── Controllers/         # API
├── Consumers/           # RabbitMQ consumer
├── Models/              # DTO
├── Services/            # бизнес-логика
└── Validators/          # валидация

# 🔧 ЗАВИСИМОСТИ
# .NET 7.0, MassTransit, RabbitMQ, Docker