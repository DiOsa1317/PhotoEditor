using System;

namespace WebApi.Models;

public class UserDto
{
    public string Login {get; }
    public string Password {get; }

    public UserDto(string Login, string Password){
        this.Login = Login;
        this.Password = Password;
    }
}
