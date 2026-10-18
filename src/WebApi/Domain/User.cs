using System;

namespace WebApi.Domain;

public class User
{
    public string Login{get; }

    public User(string Login){
        this.Login = Login;
    }
}
