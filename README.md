# PixelQueue

Сервис асинхронной обработки изображений

## Архитектура
- **WebApi** — REST API: регистрация, вход, загрузка задачи, получение результата
- **Worker** — фоновая обработка изображений (Magick.NET: негатив, зеркало, размытие, резкость)
- **SignalR** — real-time уведомления (пока шаблон)
- **MongoDB + GridFS** — хранение задач и файлов
- **RabbitMQ + MassTransit** — очередь задач

## Основной сценарий
1. Пользователь регистрируется (`POST /register`) и входит (`POST /login`)
2. Загружает изображение и выбирает фильтр (`POST /tasks`)
3. Сервер сразу отвечает со статусом `pending`
4. Worker обрабатывает изображение в фоне
5. Клиент скачивает результат (`GET /tasks/{id}/result`)

## Быстрый запуск

### 1. Инфраструктура
```bash
docker run -d --name mongodb -p 27017:27017 mongo:latest
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### 2. WebApi
```bash
dotnet run --project src/WebApi
# http://localhost:5064
```

### 3. Worker
```bash
dotnet run --project src/Worker
# http://localhost:5209
```

### 4. Проверка через curl
```bash
# Регистрация
curl -X POST http://localhost:5064/register -H "Content-Type: application/json" -d '{"login":"user1","password":"pass1"}'

# Вход
curl -X POST http://localhost:5064/login -H "Content-Type: application/json" -d '{"login":"user1","password":"pass1"}'

# Создать задачу
curl -X POST http://localhost:5064/tasks \
  -F "file=@image.jpg" \
  -F "filter=Negative" \
  -F "blurRadius=0" \
  -F "userId=<guid>"

# Проверить статус (должен стать Completed)
curl http://localhost:5064/tasks/<taskId>

# Скачать результат
curl http://localhost:5064/tasks/<taskId>/result -o result.png
```

### Сборка и тесты
```bash
dotnet build PhotoEditor.sln
dotnet test
```

## Переменные окружения
| Переменная | По умолчанию |
|------------|--------------|
| `Mongo:ConnectionString` | `mongodb://localhost:27017` |
| `Mongo:Database` | `pixelqueue` |
| `RabbitMq:Host` | `localhost` |
| `RabbitMq:Username` | `guest` |
| `RabbitMq:Password` | `guest` |
