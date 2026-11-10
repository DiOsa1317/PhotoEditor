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
        if (GetUserById(user.Id) != null)
            return false;
        _users.InsertOne(user);
        return true;
    }
}
