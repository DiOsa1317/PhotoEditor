using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Domain;

public class User
{
    [BsonConstructor]
    public User(string login, string id, string passwordHash, DateTime createdAt)
    {
        Login = login;
        Id = id;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public string Login { get; }
    public string PasswordHash { get; }
    public string Id { get; }
    public DateTime CreatedAt { get; }
}
