using System;
using Infrastructure.Interfaces;
using System.Collections.Generic;
using WebApi.Domain;

namespace Infrastructure.Repository.Ram;

public class User : IUserRepository
{
    public Dictionary<User, int> Storage{get; }

    public User(Dictionary<User, int> Storage)
    {
        this.Storage = Storage;
    }
}