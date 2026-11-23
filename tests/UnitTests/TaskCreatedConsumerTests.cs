using MassTransit;
using Moq;
using Shared.Contracts;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;
using Worker.Infrastructure.Consumers;
using Worker.Infrastructure.Interfaces;

namespace UnitTests;

public class TaskCreatedConsumerTests
{
    private static ImageTask CreateTask(string id, ImageTaskStatus status = ImageTaskStatus.Pending, int attempts = 0)
    {
        return new ImageTask
        {
            Id = id,
            UserId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            Filter = FilterType.Negative,
            BlurRadius = 0,
            Status = status,
            Attempts = attempts,
            SourceFileId = "source-file-id",
            LeaseUntil = DateTime.UtcNow.AddMinutes(5)
        };
    }

    private static Mock<ConsumeContext<TaskCreated>> CreateContext(TaskCreated message)
    {
        var context = new Mock<ConsumeContext<TaskCreated>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Test]
    public async Task ProcessPendingTask_SetsCompleted()
    {
        var task = CreateTask(Guid.NewGuid().ToString());
        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.FindByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ImageTask>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var fileStorage = new Mock<IFileStorage>();
        fileStorage.Setup(s => s.GetAsync(task.SourceFileId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Stream>(new MemoryStream(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "sample.jpg")))));

        var processor = new Mock<IImageProcessor>();
        processor.Setup(p => p.ProcessAsync(It.IsAny<byte[]>(), It.IsAny<FilterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var consumer = new TaskCreatedConsumer(taskRepo.Object, fileStorage.Object,
            processor.Object, publishEndpoint.Object);

        var context = CreateContext(new TaskCreated(Guid.Parse(task.Id),
            Guid.Parse(task.SessionId), task.Filter, task.BlurRadius, task.SourceFileId));

        await consumer.Consume(context.Object);

        taskRepo.Verify(r => r.UpdateAsync(It.Is<ImageTask>(t =>
            t.Status == ImageTaskStatus.Completed), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        publishEndpoint.Verify(p => p.Publish(It.IsAny<TaskCompleted>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CompletedTask_IsIdempotent()
    {
        var task = CreateTask(Guid.NewGuid().ToString(), ImageTaskStatus.Completed);
        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.FindByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        
        var consumer = new TaskCreatedConsumer(taskRepo.Object, new  Mock<IFileStorage>().Object,
            new Mock<IImageProcessor>().Object, new Mock<IPublishEndpoint>().Object);

        var context = CreateContext(new TaskCreated(Guid.Parse(task.Id), Guid.Parse(task.SessionId),
            FilterType.Negative, 0, "source"));
        
        await consumer.Consume(context.Object);
        
        taskRepo.Verify(r => r.UpdateAsync(It.IsAny<ImageTask>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task MaxAttempts_SetsFailed()
    {
        var task = CreateTask(Guid.NewGuid().ToString(), ImageTaskStatus.Pending, attempts: 3);
        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.FindByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ImageTask>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var consumer = new TaskCreatedConsumer(taskRepo.Object, new  Mock<IFileStorage>().Object,
            new Mock<IImageProcessor>().Object, publishEndpoint.Object);
        var context = CreateContext(new TaskCreated(Guid.Parse(task.Id),Guid.NewGuid(),
            FilterType.Negative, 0, "source"));
        await consumer.Consume(context.Object);
        taskRepo.Verify(r => r.UpdateAsync(It.Is<ImageTask>(t
                => t.Status == ImageTaskStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        publishEndpoint.Verify(p => p.Publish(It.IsAny<TaskFailed>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}