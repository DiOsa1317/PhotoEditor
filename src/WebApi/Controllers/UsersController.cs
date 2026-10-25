using System;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using Infrastructure.Repository.Ram;
using WebApi.Domain;
using Infrastructure.Interfaces;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    public IUserRepository UserStorage{get; } //пока нет бизнес логики взаимодействую напрямую через слой Repository
    public ISessionRepository SessionStorage { get; }

    public UsersController(IUserRepository Storage, ISessionRepository SessionStorage)
    {
        this.UserStorage = Storage;
        this.SessionStorage = SessionStorage;
    }

    [HttpGet()]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromQuery] string userId)
    {
        // прокидываю в слой бизнес логики в будущем и там валидирую
        var user = UserStorage.GetUserById(userId);
        if (user == null)
            return NotFound();
        
        var userDto = new UserDto(user.Login, user.Id);
        return Ok(userDto);
    }

    [HttpPost("/register")]
    [Produces("application/json", "application/xml")]
    public ActionResult RegisterUser([FromBody] UserDto userDto)
    {
        if (UserStorage.GetUserById(userDto.Id) != null)
        {
            return BadRequest();
        }

        // прокидываю в слой бизнес логики в будущем
        // и формирую там сущность, но пока что здесь

        var user = new User(userDto.Login, userDto.Id); //надо сделать автомаппер
        
        if (!UserStorage.PostUser(user))
            return BadRequest();
        return Created();
    }

    [HttpPost("/login")]
    [Produces("application/json", "application/xml")]
    public ActionResult<SessionDto> LoginUser([FromBody] UserDto userDto)
    {
        if (UserStorage.GetUserById(userDto.Id) == null)
            return BadRequest();
        // а также проверка в бизнесе логина и пароля
        var sessionId = "12345"; //в бизнесе генерится новая сессия а пока заглушка
        var user = new User(userDto.Login, userDto.Id); // опять же автомаппер + в бизнесе это все
        if (!SessionStorage.PostSession(sessionId, user))
            return BadRequest();
        return Ok(new SessionDto(sessionId));
    }
}

