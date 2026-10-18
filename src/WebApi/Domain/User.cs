using System;

namespace WebApi.Domain;

public class User
{
    public string Login{get; }
    public string Id{get; }

    public User(string Login){
        this.Login = Login;
        this.Id = Id;
    }
}
