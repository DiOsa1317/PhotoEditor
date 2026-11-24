using MongoDB.Driver;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Mongo;

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
        _users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(user => user.Login),
            new CreateIndexOptions
            {
                Unique = true,
                Name = "ux_users_login"
            }));
    }

    public User? GetUserById(string userId)
    {
        return _users.Find(u => u.Id == userId).FirstOrDefault();
    }

    public User? GetUserByLogin(string login)
    {
        return _users.Find(u => u.Login == login).FirstOrDefault();
    }

    public bool PostUser(User user)
    {
        try
        {
            _users.InsertOne(user);
            return true;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}