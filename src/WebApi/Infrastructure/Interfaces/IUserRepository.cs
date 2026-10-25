using System;
using WebApi.Domain;
using WebApi.Models;

namespace Infrastructure.Interfaces;

public interface IUserRepository
{
    public User? GetUserById(string userId);
    public bool PostUser(User user);
}
