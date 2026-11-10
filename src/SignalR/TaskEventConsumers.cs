using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Shared.Contracts;

namespace Notifications;

public class TaskCompletedConsumer : IConsumer<TaskCompleted>
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ILogger<TaskCompletedConsumer> _logger;

    public TaskCompletedConsumer(IHubContext<NotificationsHub> hub, ILogger<TaskCompletedConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCompleted> context)
    {
        using (CorrelationHelper.Push(context))
        {
            var message = context.Message;
            await _hub.Clients.Group(message.UserId.ToString())
                .SendAsync("TaskCompleted",
                    new { taskId = message.TaskId, resultFileId = message.ResultFileId },
                    context.CancellationToken);

            _logger.LogInformation("Notification sent: task {TaskId} completed for user {UserId}",
                message.TaskId, message.UserId);
        }
    }
}

public class TaskFailedConsumer : IConsumer<TaskFailed>
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ILogger<TaskFailedConsumer> _logger;

    public TaskFailedConsumer(IHubContext<NotificationsHub> hub, ILogger<TaskFailedConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskFailed> context)
    {
        using (CorrelationHelper.Push(context))
        {
            var message = context.Message;
            await _hub.Clients.Group(message.UserId.ToString())
                .SendAsync("TaskFailed",
                    new { taskId = message.TaskId, error = message.Error },
                    context.CancellationToken);

            _logger.LogInformation("Notification sent: task {TaskId} failed for user {UserId}",
                message.TaskId, message.UserId);
        }
    }
}