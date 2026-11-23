namespace Shared.Contracts;

public record TaskCompleted(Guid TaskId, Guid SessionId, string ResultFileId);