using System;

namespace Shared.Domain;

public class User
{
    public string Login{get; }
    public string Id{get; }

    public User(string Login, string Id){
        this.Login = Login;
        this.Id = Id;
    }
}
