using Shared.Domain;

namespace Shared.Contracts;

public record TaskCreated(Guid TaskId, Guid UserId, FilterType Filter, double BlurRadius, string SourceFileId);