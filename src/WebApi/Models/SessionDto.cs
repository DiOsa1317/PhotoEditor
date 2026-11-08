

namespace WebApi.Models;

public class SessionDto
{
    public string SessionId { get; }

    public SessionDto(string SessionId)
    {
        this.SessionId = SessionId;
    }
}