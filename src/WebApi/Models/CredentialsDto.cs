namespace WebApi.Models;

public class CredentialsDto
{
    public CredentialsDto(string login, string password)
    {
        Login = login;
        Password = password;
    }

    public string Login { get; }
    public string Password { get; }
}