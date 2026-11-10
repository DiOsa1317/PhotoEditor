# Телеметрия

## Логи
- Библиотека: Serilog, вывод в консоль в формате JSON (Compact)
- На каждый HTTP-запрос пишется одна строка: метод, путь, код ответа, время (мс)
- Поле `@tr` в строке это traceId, сквозной идентификатор запроса
- Дальше: сбор логов в ELK, просмотр в Kibana 
- В каждой строке лога WebApi есть `CorrelationId`: берётся из заголовка `X-Correlation-Id`, а если его нет, создаётся новый. Тот же номер возвращается в ответе

## Метрики
- Библиотека: prometheus-net.AspNetCore, адрес `/metrics`
- Основные метрики:
    - `http_requests_received_total`: сколько запросов обработано (по методу, адресу и коду ответа);
    - `http_request_duration_seconds`: время ответа;
    - `http_requests_in_progress`: сколько запросов обрабатывается сейчас.
- Также автоматически собираются метрики процесса: память, процессор, потоки.
- Дальше: сбор Prometheus, свои метрики (`tasks_pending_total`, время обработки задачи)

## События очереди
- `TaskCreated`, `TaskCompleted`, `TaskFailed`
- В `TaskCompleted` и `TaskFailed` передаются поля `taskId`, `userId`, `status`.

## Трассировка (планируется)
- OpenTelemetry и Jaeger: путь запроса WebApi → RabbitMQ → Worker → MongoDB → SignalR