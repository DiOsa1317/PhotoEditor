namespace WebApi.Models;

public class UserDto
{
    public string Login {get; }
    public string Id {get; }

    public UserDto(string login, string id){
        Login = login;
        Id = id;
    }
}
