using Shared.Infrastructure.Interfaces;
using Shared.Domain;

namespace Shared.Infrastructure.Repository.Ram;

public class RamSessionRepository : ISessionRepository
{
    public Dictionary<string, User> Storage { get; }

    public RamSessionRepository()
    {
        Storage = new Dictionary<string, User>();
    }
    
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