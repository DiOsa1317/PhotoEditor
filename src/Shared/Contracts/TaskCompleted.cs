namespace Shared.Contracts;

public record TaskCompleted(Guid TaskId, Guid UserId, string ResultFileId);