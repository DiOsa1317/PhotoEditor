using System.ComponentModel.DataAnnotations;

namespace WebApi.Models;

public class CredentialsDto
{
    public CredentialsDto(string login, string password)
    {
        Login = login;
        Password = password;
    }

    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string Login { get; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; }
}