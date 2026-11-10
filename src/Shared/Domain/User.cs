namespace Shared.Domain;

public class User
{
    public User(string login, string id, string passwordHash, DateTime? createdAt = null)
    {
        Login = login;
        Id = id;
        PasswordHash = passwordHash;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public string Login { get; }
    public string PasswordHash { get; }
    public string Id { get; }
    public DateTime CreatedAt { get; }
}
