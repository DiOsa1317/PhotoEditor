using System;
using Infrastructure.Interfaces;
using System.Collections.Generic;
using WebApi.Domain;

namespace Infrastructure.Repository.Ram;

public class RamUserRepository : IUserRepository
{
    public Dictionary<string, User> Storage{get; }

    public RamUserRepository()
    {
        this.Storage = new Dictionary<string, User>();
    }

    public User? GetUserById(string userId)
    {
        if (!Storage.ContainsKey(userId))
            return null;
        return Storage[userId];
    }

    public bool PostUser(User user)
    {
        if (GetUserById(user.Id) != null)
            return false;
        Storage[user.Id] = user;
        return true;
    }
}