using System;
using WebApi.Domain;

namespace Infrastructure.Interfaces;

public interface IUserRepository
{
    public User? GetUserById(string userId);
}
