using System;

namespace WebApi.Models;

public class UserDto
{
    public string Login {get; }
    public int Id {get; }

    public UserDto(string login, int id){
        Login = login;
        Id = id;
    }
}
