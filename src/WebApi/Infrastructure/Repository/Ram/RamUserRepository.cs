using System;
using Infrastructure.Interfaces;
using System.Collections.Generic;
using WebApi.Domain;

namespace Infrastructure.Repository.Ram;

public class RamUserRepository : IUserRepository
{
    public Dictionary<string, User> Storage{get; }

    public RamUserRepository(Dictionary<string, User> Storage)
    {
        this.Storage = Storage;
    }

    public User? GetUserById(string userId)
    {
        if (!Storage.ContainsKey(userId))
            return null;
        return Storage[userId];
    }
}