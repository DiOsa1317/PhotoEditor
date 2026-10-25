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

    public UsersController(IUserRepository Storage)
    {
        this.UserStorage = Storage;
    }

    [HttpGet()]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromQuery] string userId)
    {
        // прокидываю в слой бизнес логики в будущем
        var user = UserStorage.GetUserById(userId);
        if (user == null)
            return NotFound();

        var userDto = new UserDto(user.Login, user.Id);
        return Ok(userDto);
    }

    [HttpPost()]
    [Produces("application/json", "application/xml")]
    public ActionResult PostUser([FromBody] UserDto userDto)
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
        return Ok();
    }
}

