using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Mongo;

public class MongoSessionRepository : ISessionRepository
{
    private readonly IMongoCollection<UserSessionDocument> _sessions;
    private readonly IMongoCollection<User> _users;

    public MongoSessionRepository(IMongoDatabase database)
    {
        _sessions = database.GetCollection<UserSessionDocument>("sessions");
        _users = database.GetCollection<User>("users");
    }

    public User? GetUserBySessionId(string sessionId)
    {
        var session = _sessions.Find(s => s.SessionId == sessionId).FirstOrDefault();
        return session == null
            ? null
            : _users.Find(u => u.Id == session.UserId).FirstOrDefault();
    }

    public bool PostSession(string sessionId, User user)
    {
        try
        {
            _sessions.InsertOne(new UserSessionDocument
            {
                SessionId = sessionId,
                UserId = user.Id
            });
            return true;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

public class UserSessionDocument
{
    [BsonId]
    public string SessionId { get; set; } = null!;

    public string UserId { get; set; } = null!;
}
