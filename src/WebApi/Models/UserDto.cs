using System;

namespace WebApi.Models;

public class UserDto
{
    public string Login {get; }
    public string Id {get; }

    public UserDto(string Login, string Id){
        this.Login = Login;
        this.Id = Id;
    }
}
