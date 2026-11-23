using Shared.Domain;

namespace Shared.Contracts;

public record TaskCreated(Guid TaskId, Guid SessionId, FilterType Filter, double BlurRadius, string SourceFileId);