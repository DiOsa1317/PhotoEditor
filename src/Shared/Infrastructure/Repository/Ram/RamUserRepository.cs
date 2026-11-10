using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Repository.Ram;

public class RamUserRepository : IUserRepository
{
    public RamUserRepository()
    {
        Storage = new Dictionary<string, User>();
    }

    public Dictionary<string, User> Storage { get; }

    public User? GetUserById(string userId)
    {
        return Storage.GetValueOrDefault(userId);
    }

    public User? GetUserByLogin(string login)
    {
        return Storage.Values.FirstOrDefault(x => x.Login == login);
    }

    public bool PostUser(User user)
    {
        if (GetUserById(user.Id) != null)
            return false;
        Storage[user.Id] = user;
        return true;
    }
}