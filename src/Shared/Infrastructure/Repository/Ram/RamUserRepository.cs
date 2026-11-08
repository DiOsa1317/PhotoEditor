using Shared.Infrastructure.Interfaces;
using Shared.Domain;

namespace Shared.Infrastructure.Repository.Ram;

public class RamUserRepository : IUserRepository
{
    public Dictionary<string, User> Storage{get; }

    public RamUserRepository()
    {
        this.Storage = new Dictionary<string, User>();
    }

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