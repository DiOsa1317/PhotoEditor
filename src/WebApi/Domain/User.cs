using System;

namespace WebApi.Domain;

public class User
{
    public string Login{get; }
    public string PasswordHash { get; }
    public string Id{get; }

    public User(string Login, string Id, string PasswordHash){
        this.Login = Login;
        this.Id = Id;
        this.PasswordHash = PasswordHash;
    }
}
