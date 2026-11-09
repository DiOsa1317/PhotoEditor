namespace Shared.Contracts;

public record TaskFailed(Guid TaskId, Guid UserId, string Error);