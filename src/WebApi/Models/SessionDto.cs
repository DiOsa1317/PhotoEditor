namespace WebApi.Models;

public class SessionDto
{
    public SessionDto(string SessionId)
    {
        this.SessionId = SessionId;
    }

    public string SessionId { get; }
}