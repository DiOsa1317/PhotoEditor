using System;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using Infrastructure.Repository.Ram;
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
        var user = UserStorage.GetUserById(userId);
        if (user == null)
            return NotFound();

        var userDto = new UserDto(user.Login, user.Id);
        return Ok(userDto);
    }

    // [HttpPost()]
    // [Produces("application/json", "application/xml")]
    // public ActionResult RegisterUser([FromBody] CreateUserDto? user)
    // {
    //     if (user == null)
    //         return BadRequest();
        
    // }
}

