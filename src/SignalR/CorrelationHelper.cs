using MassTransit;

namespace Notifications;

public static class CorrelationHelper
{
    public const string HeaderName = "X-Correlation-Id";

    // Кладёт CorrelationId в логи, пока объект не будет освобождён (using)
    public static IDisposable Push(ConsumeContext context)
    {
        var id = context.Headers.Get<string>(HeaderName)
                 ?? context.ConversationId?.ToString()
                 ?? "unknown";
        return Serilog.Context.LogContext.PushProperty("CorrelationId", id);
    }
}