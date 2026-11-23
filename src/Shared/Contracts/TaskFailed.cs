namespace Shared.Contracts;

public record TaskFailed(Guid TaskId, Guid SessionId, string Error);