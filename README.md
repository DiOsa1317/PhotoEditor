# PixelQueue

Сервис асинхронной обработки изображений

## Архитектура

- **WebApi** — REST API: регистрация, вход, загрузка задачи, получение результата
- **Worker** — фоновая обработка изображений (Magick.NET: негатив, зеркало, размытие, резкость)
- **SignalR** — сервис уведомлений: слушает события `TaskCompleted` и `TaskFailed` из RabbitMQ и отправляет их пользователю через хаб `/hubs/notifications`. Внутри лежит простой веб-клиент (`src/SignalR/wwwroot`)
- **MongoDB + GridFS** — хранение задач и файлов
- **RabbitMQ + MassTransit** — очередь задач

## Основной сценарий

1. Пользователь регистрируется (`POST /register`) и входит (`POST /login`)
2. Загружает изображение и выбирает фильтр (`POST /tasks`)
3. Сервер сразу отвечает со статусом `pending`
4. Worker обрабатывает изображение в фоне
5. Когда Worker закончил, SignalR-сервис присылает клиенту уведомление (`TaskCompleted` или `TaskFailed`)
6. Клиент скачивает результат (`GET /tasks/{id}/result`)

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

### 4. SignalR и веб-клиент

```bash
dotnet run --project src/SignalR
# http://localhost:5228
```

Открой `http://localhost:5228`, выбери картинку и фильтр. Результат появится на странице сам, когда Worker закончит обработку. Для работы клиента WebApi должен быть запущен.

### 5. Проверка через curl

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

## Запуск в Kubernetes (minikube)

Манифесты (`Deployment`, `Service`, `ConfigMap`, `Secret`, `PersistentVolumeClaim`) находятся в папке `Deploy/`.

**Требования:** установленные `docker`, `minikube` и `kubectl`.

### Первый запуск

1. Запусти кластер:
   ```bash
   minikube start
   ```
2. Собери Docker-образы сервисов (контекст сборки — папка `src`):
   ```bash
   docker build -t pixelqueue-webapi:latest -f src/WebApi/Dockerfile src
   docker build -t pixelqueue-worker:latest -f src/Worker/Dockerfile src
   docker build -t pixelqueue-signalr:latest -f src/SignalR/Dockerfile src
   ```
3. Загрузи образы в minikube:
   ```bash
   minikube image load pixelqueue-webapi:latest
   minikube image load pixelqueue-worker:latest
   minikube image load pixelqueue-signalr:latest
   ```

   *(Если Docker требует `sudo`, передай образы через pipe: `sudo docker save pixelqueue-webapi:latest | minikube image load -` и аналогично для остальных сервисов.)*
4. Создай локальный файл секретов из шаблона (файл `Deploy/secret.yaml` добавлен в `.gitignore`):
   ```bash
   cp Deploy/secret.yaml.example Deploy/secret.yaml
   ```
5. Примени манифесты и дождись, пока все поды перейдут в статус `Running` (`1/1`):
   ```bash
   kubectl apply -f Deploy/
   kubectl get pods -w
   ```
6. Пробрось порты сервисов на локальную машину:
   ```bash
   kubectl port-forward svc/webapi 5000:80 &
   kubectl port-forward svc/signalr 5001:80 &
   kubectl port-forward svc/rabbitmq 15672:15672 &
   ```

После запуска доступны:

- **Веб-клиент (SignalR):** `http://localhost:5001`
- **WebApi:** `http://localhost:5000`
- **RabbitMQ Management UI:** `http://localhost:15672` (`guest` / `guest`)

### Повторный запуск (после `minikube stop` или перезагрузки)

Образы, манифесты и данные MongoDB сохраняются внутри кластера:

```bash
minikube start
kubectl get pods -w
kubectl port-forward svc/webapi 5000:80 &
kubectl port-forward svc/signalr 5001:80 &
kubectl port-forward svc/rabbitmq 15672:15672 &
```

### Обновление после изменений в коде

Пересобери изменённые сервисы, загрузи образы в кластер и перезапусти деплойменты:

```bash
docker build -t pixelqueue-webapi:latest -f src/WebApi/Dockerfile src
docker build -t pixelqueue-worker:latest -f src/Worker/Dockerfile src
docker build -t pixelqueue-signalr:latest -f src/SignalR/Dockerfile src

minikube image load pixelqueue-webapi:latest
minikube image load pixelqueue-worker:latest
minikube image load pixelqueue-signalr:latest

kubectl rollout restart deployment webapi worker signalr
kubectl get pods -w
```

Затем перезапусти `port-forward`:

```bash
pkill -f "kubectl port-forward"
kubectl port-forward svc/webapi 5000:80 &
kubectl port-forward svc/signalr 5001:80 &
kubectl port-forward svc/rabbitmq 15672:15672 &
```

### Остановка и очистка

```bash
# Остановить проброс портов и кластер (данные сохранятся)
pkill -f "kubectl port-forward"
minikube stop

# Полностью удалить ресурсы проекта из кластера
kubectl delete -f Deploy/
```

### Сборка и тесты

```bash
dotnet build PhotoEditor.sln
dotnet test
```

## Переменные окружения

| Переменная       | По умолчанию       |
| -------------------------- | ----------------------------- |
| `Mongo:ConnectionString` | `mongodb://localhost:27017` |
| `Mongo:Database`         | `pixelqueue`                |
| `RabbitMq:Host`          | `localhost`                 |
| `RabbitMq:Username`      | `guest`                     |
| `RabbitMq:Password`      | `guest`                     |
