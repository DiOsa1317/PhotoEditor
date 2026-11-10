using MassTransit;
using Shared.Contracts;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;
using Worker.Infrastructure.Interfaces;

namespace Worker.Infrastructure.Consumers;

public class TaskCreatedConsumer : IConsumer<TaskCreated>
{
    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITaskRepository _taskRepository;

    public TaskCreatedConsumer(
        ITaskRepository taskRepository,
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        IPublishEndpoint publishEndpoint)
    {
        _taskRepository = taskRepository;
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<TaskCreated> context)
    {
        var message = context.Message;
        var task = await _taskRepository.FindByIdAsync(message.TaskId.ToString(), context.CancellationToken);
        if (task == null) return;

        task.Status = ImageTaskStatus.Processing;
        task.Attempts += 1;
        task.LastUpdated = DateTime.UtcNow;
        task.LeaseOwner = Guid.NewGuid().ToString();
        task.LeaseUntil = DateTime.UtcNow.AddMinutes(5);
        await _taskRepository.UpdateAsync(task, context.CancellationToken);

        try
        {
            await using var sourceStream = await _fileStorage.GetAsync(task.SourceFileId, context.CancellationToken);
            using var memoryStream = new MemoryStream();
            await sourceStream.CopyToAsync(memoryStream, context.CancellationToken);
            memoryStream.Position = 0;

            var resultBytes = await _imageProcessor.ProcessAsync(
                memoryStream.ToArray(),
                new FilterRequest(task.Filter, task.BlurRadius),
                context.CancellationToken);

            using var resultStream = new MemoryStream(resultBytes);
            var resultFileId = await _fileStorage.SaveAsync(resultStream,
                $"{task.Id}_{task.Filter}.png", "image/png", context.CancellationToken);

            task.Status = ImageTaskStatus.Completed;
            task.ResultFileId = resultFileId;
            task.LastUpdated = DateTime.UtcNow;
            task.LeaseOwner = string.Empty;
            task.LeaseUntil = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task, context.CancellationToken);

            await _publishEndpoint.Publish(new TaskCompleted(Guid.Parse(task.Id), Guid.Parse(task.UserId), task.ResultFileId),
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            task.Status = ImageTaskStatus.Failed;
            task.Error = ex.Message;
            task.LastUpdated = DateTime.UtcNow;
            task.LeaseOwner = string.Empty;
            task.LeaseUntil = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task, context.CancellationToken);
            await _publishEndpoint.Publish(new TaskFailed(Guid.Parse(task.Id), Guid.Parse(task.UserId), ex.Message), context.CancellationToken);
        }
    }
}