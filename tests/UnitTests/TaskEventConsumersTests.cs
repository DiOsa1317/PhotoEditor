using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Notifications;
using Shared.Contracts;

namespace UnitTests;

public class TaskEventConsumersTests
{
    private Mock<IClientProxy> _groupProxy = null!;
    private Mock<IHubClients> _clients = null!;
    private Mock<IHubContext<NotificationsHub>> _hub = null!;
    private object?[]? _sentArgs;

    [SetUp]
    public void SetUp()
    {
        _sentArgs = null;

        _groupProxy = new Mock<IClientProxy>();
        _groupProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => _sentArgs = args)
            .Returns(Task.CompletedTask);

        _clients = new Mock<IHubClients>();
        _clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupProxy.Object);

        _hub = new Mock<IHubContext<NotificationsHub>>();
        _hub.Setup(h => h.Clients).Returns(_clients.Object);
    }

    [Test]
    public async Task TaskCompleted_IsSentToUsersGroup()
    {
        var message = new TaskCompleted(Guid.NewGuid(), Guid.NewGuid(), "file-123");
        var consumer = new TaskCompletedConsumer(_hub.Object, NullLogger<TaskCompletedConsumer>.Instance);

        await consumer.Consume(ContextFor(message));

        _clients.Verify(c => c.Group(message.UserId.ToString()), Times.Once);
        _groupProxy.Verify(p => p.SendCoreAsync("TaskCompleted", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);

        var payload = JsonSerializer.Serialize(_sentArgs![0]);
        Assert.Multiple(() =>
        {
            Assert.That(payload, Does.Contain(message.TaskId.ToString()));
            Assert.That(payload, Does.Contain("file-123"));
        });
    }

    [Test]
    public async Task TaskFailed_IsSentToUsersGroup()
    {
        var message = new TaskFailed(Guid.NewGuid(), Guid.NewGuid(), "boom");
        var consumer = new TaskFailedConsumer(_hub.Object, NullLogger<TaskFailedConsumer>.Instance);

        await consumer.Consume(ContextFor(message));

        _clients.Verify(c => c.Group(message.UserId.ToString()), Times.Once);
        _groupProxy.Verify(p => p.SendCoreAsync("TaskFailed", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);

        var payload = JsonSerializer.Serialize(_sentArgs![0]);
        Assert.Multiple(() =>
        {
            Assert.That(payload, Does.Contain(message.TaskId.ToString()));
            Assert.That(payload, Does.Contain("boom"));
        });
    }

    private static ConsumeContext<T> ContextFor<T>(T message) where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }
}