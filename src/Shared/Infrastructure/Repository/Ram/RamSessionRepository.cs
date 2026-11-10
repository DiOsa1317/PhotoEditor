using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Repository.Ram;

public class RamSessionRepository : ISessionRepository
{
    public RamSessionRepository()
    {
        Storage = new Dictionary<string, User>();
    }

    public Dictionary<string, User> Storage { get; }

    public User? GetUserBySessionId(string sessionId)
    {
        return Storage.GetValueOrDefault(sessionId);
    }

    public bool PostSession(string sessionId, User user)
    {
        if (GetUserBySessionId(sessionId) != null)
            return false;
        Storage[sessionId] = user;
        return true;
    }
}