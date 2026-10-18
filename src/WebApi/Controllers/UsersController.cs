using System;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    [HttpGet()]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromQuery] int userId)
    {
        var userDto = new UserDto("Alex", userId);
        if (userDto == null)
            return NotFound();
        return Ok(userDto);
    }
}

