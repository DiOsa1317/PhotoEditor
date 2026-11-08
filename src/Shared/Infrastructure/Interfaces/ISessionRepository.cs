using Shared.Domain;

namespace Shared.Infrastructure.Interfaces;

public interface ISessionRepository
{
    public User? GetUserBySessionId(string sessionId);
    public bool PostSession(string sessionId, User user);
}