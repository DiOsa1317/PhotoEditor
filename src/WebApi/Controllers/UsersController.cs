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

    public IUserRepository UserStorage { get; } //пока нет бизнес логики взаимодействую напрямую через слой Repository
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

        var user = new User(dto.Login, Guid.NewGuid().ToString(), "hash", DateTime.UtcNow); //надо сделать автомаппер 

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
        // if (UserStorage.GetUserById(userDto.Id) == null) //надо поправить обработку
        //     return BadRequest();
        // а также проверка в бизнесе логина и пароля
        var user = UserStorage.GetUserByLogin(dto.Login); // опять же автомаппер + в бизнесе это все
        if (user == null) return BadRequest();
        var sessionId = Guid.NewGuid().ToString(); //в бизнесе генерится новая сессия а пока заглушка
        if (!SessionStorage.PostSession(sessionId, user))
            return BadRequest();
        return Ok(new SessionDto(sessionId));
    }
}