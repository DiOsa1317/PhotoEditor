namespace WebApi.Models;

public class UserDto
{
    public UserDto(string login, string id)
    {
        Login = login;
        Id = id;
    }

    public string Login { get; }
    public string Id { get; }
}