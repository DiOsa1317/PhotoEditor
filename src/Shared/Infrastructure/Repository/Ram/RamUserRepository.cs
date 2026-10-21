using System;
using Shared.Infrastructure.Interfaces;
using System.Collections.Generic;
using Shared.Domain;

namespace Shared.Infrastructure.Repository.Ram;

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