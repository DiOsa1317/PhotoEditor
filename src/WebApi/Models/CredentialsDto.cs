namespace WebApi.Models;

public class CredentialsDto
{
    public string Login { get; }
    public string Password { get; }

    public CredentialsDto(string login, string password)
    {
        Login = login;
        Password = password;
    }
}