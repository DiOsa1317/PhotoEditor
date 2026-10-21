using System;
using Shared.Domain;

namespace Shared.Infrastructure.Interfaces;

public interface IUserRepository
{
    public User? GetUserById(string userId);
}
