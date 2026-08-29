using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GReSym.API.Extensions;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _service;

    public UsersController(IUsersService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserInfoResponseDto), 200)]
    [ProducesResponseType(typeof(string), 401)]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        int userId = User.GetUserId();

        try
        {
            var user = await _service.GetUserInfo(userId);

            return Ok(user);
        }
        catch (UserNotFoundException e)
        {
            return Unauthorized(e.Message);
        }
    }

    [HttpPatch("me")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserInfoResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [Authorize]
    public async Task<IActionResult> PatchUsername([FromBody] UpdateUsernameRequestDto request)
    {
        int userId = User.GetUserId();

        try
        {
            var user = await _service.SetUsername(userId, request.Username);

            return Ok(user);
        }
        catch (UserNotFoundException e)
        {
            return Unauthorized(e.Message);
        }
        catch (UsernameShortException e)
        {
            return BadRequest(e.Message);
        }
    }
}