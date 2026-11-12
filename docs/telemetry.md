# Телеметрия

## Логи
- Библиотека: Serilog, вывод в консоль в формате JSON (Compact)
- На каждый HTTP-запрос пишется одна строка: метод, путь, код ответа, время (мс)
- Поле `@tr` это traceId (идентификатор трассы OpenTelemetry), `@sp` это spanId. По traceId из лога можно найти трассу в Jaeger
- Дальше: сбор логов в ELK, просмотр в Kibana 
- В каждой строке лога WebApi есть `CorrelationId`: берётся из заголовка `X-Correlation-Id`, а если его нет, создаётся новый. Тот же номер возвращается в ответе
- В SignalR `CorrelationId` пишется в логах консьюмеров: берётся из заголовка сообщения `X-Correlation-Id`, а если его нет, используется `ConversationId`, который MassTransit присваивает цепочке сообщений

## Метрики
- Библиотека: prometheus-net.AspNetCore, адрес `/metrics`
- Метрики и JSON-логи есть у всех трёх сервисов: WebApi (5064), Worker (5209), SignalR (5228)
- Основные метрики:
    - `http_requests_received_total`: сколько запросов обработано (по методу, адресу и коду ответа);
    - `http_request_duration_seconds`: время ответа;
    - `http_requests_in_progress`: сколько запросов обрабатывается сейчас.
- Также автоматически собираются метрики процесса: память, процессор, потоки.
- Свои метрики описаны в `Shared.Telemetry.TaskMetrics`:
    - `tasks_pending_total` (gauge): сколько задач ждёт или обрабатывается;
    - `tasks_failed_total` (counter): сколько задач завершилось ошибкой;
    - `job_duration_seconds` (histogram): время обработки задачи, корзины от 0,05 с с удвоением (10 штук).
- Класс готов; вызовы `TaskReceived()`, `TaskCompleted(seconds)`, `TaskFailed()` планируется добавить в Worker (`TaskCreatedConsumer`)
- Дальше: сбор Prometheus, метрика длины очередей

## Проверка состояния (SignalR)
- `/health/live`: процесс жив (200, если сервис отвечает). Нужен для liveness-пробы Kubernetes
- `/health/ready`: сервис готов принимать трафик. Проверяет соединение с RabbitMQ: если шина недоступна, отвечает 503. Нужен для readiness-пробы
- Эндпоинты `/metrics` и `/health` не попадают в трассы, чтобы их частый опрос не засорял Jaeger

## События очереди
- `TaskCreated` (TaskId, UserId, Filter, BlurRadius, SourceFileId): WebApi публикует после создания задачи
- `TaskCompleted` (TaskId, UserId, ResultFileId): Worker публикует после успешной обработки
- `TaskFailed` (TaskId, UserId, Error): Worker публикует при ошибке
- SignalR-сервис получает `TaskCompleted` и `TaskFailed` и пересылает их пользователю

## Трассировка
- OpenTelemetry, трассы отправляются по OTLP в Jaeger. Подключено во всех трёх сервисах (`webapi`, `worker`, `signalr`)
- В трассы попадают HTTP-запросы, отправка и обработка сообщений MassTransit (`send`, `receive`, `process`) и запросы к MongoDB (источник `MongoDB.Driver`, он встроен в драйвер версии 3.7 и выше). Текст запросов к базе в трассы не попадает
- Контекст трассы передаётся между сервисами через заголовки сообщений RabbitMQ, поэтому трасса получается сквозной без дополнительного кода
- Адрес сборщика задаётся переменной окружения `OTEL_EXPORTER_OTLP_ENDPOINT` (по умолчанию `http://localhost:4317`)
- Локальный запуск Jaeger:
  ```bash
  docker run -d --name jaeger -p 16686:16686 -p 4317:4317 -p 4318:4318 jaegertracing/all-in-one:latest
  ```
  Интерфейс: http://localhost:16686, выбрать сервис `webapi`, нажать Find Traces и открыть трассу `POST tasks`
