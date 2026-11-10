using Microsoft.AspNetCore.Mvc;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;
using WebApi.Models;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    public UsersController(IUserRepository userStorage, ISessionRepository sessionStorage)
    {
        UserStorage = userStorage;
        SessionStorage = sessionStorage;
    }

    public IUserRepository UserStorage { get; }
    public ISessionRepository SessionStorage { get; }

    [HttpGet]
    public ActionResult<UserDto> GetUserBYId([FromQuery] string userId)
    {
        var user = UserStorage.GetUserById(userId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Login, user.Id));
    }

    // [HttpGet()]
    // [Produces("application/json", "application/xml")]
    // public ActionResult<UserDto> GetUserByLogin([FromQuery] string login) // возможно уберу метод
    // {
    //     // прокидываю в слой бизнес логики в будущем и там валидирую
    //     // var user = UserStorage.GetUserById(userId);
    //     // if (user == null)
    //     //     return NotFound();
    //     
    //     var userDto = new UserDto(user.Login, "passwordBase");
    //     return Ok(userDto);
    // }

    [HttpPost("/register")]
    [Produces("application/json", "application/xml")]
    public ActionResult RegisterUser([FromBody] CredentialsDto dto)
    {
        // прокидываю в слой бизнес логики в будущем
        // и формирую там сущность с хешем пароля, но пока что здесь

        var user = new User(dto.Login, Guid.NewGuid().ToString(), "hash", DateTime.UtcNow);

        // if (UserStorage.GetUserByLogin(user) != null)
        // {
        //     return BadRequest();
        // }

        if (!UserStorage.PostUser(user))
            return BadRequest();
        return Created();
    }

    [HttpPost("/login")]
    [Produces("application/json", "application/xml")]
    public ActionResult<SessionDto> LoginUser([FromBody] CredentialsDto dto)
    {
        var user = UserStorage.GetUserByLogin(dto.Login);
        if (user == null) return BadRequest();
        var sessionId = Guid.NewGuid().ToString();
        if (!SessionStorage.PostSession(sessionId, user))
            return BadRequest();
        return Ok(new SessionDto(sessionId));
    }
}