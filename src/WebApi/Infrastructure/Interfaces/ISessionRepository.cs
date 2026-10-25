using WebApi.Domain;

namespace Infrastructure.Interfaces;

public interface ISessionRepository
{
    public User? GetUserBySessionId(string sessionId);
    public bool PostSession(string sessionId, User user);
}