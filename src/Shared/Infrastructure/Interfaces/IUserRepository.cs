using Shared.Domain;

namespace Shared.Infrastructure.Interfaces;

public interface IUserRepository
{
    public User? GetUserById(string userId);

    public User? GetUserByLogin(string login);

    public bool PostUser(User user);
}