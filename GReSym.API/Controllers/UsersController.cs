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
    private readonly IUserSettingsService _settingsService;

    public UsersController(IUsersService service, IUserSettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;
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

    [HttpGet("me/settings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserSettingsDto), 200)]
    [Authorize]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetSettings(User.GetUserId());

        return Ok(settings);
    }

    [HttpPatch("me/settings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserSettingsDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [Authorize]
    public async Task<IActionResult> PatchSettings([FromBody] UpdateUserSettingsRequestDto request)
    {
        var settings = await _settingsService.UpdateSettings(User.GetUserId(), request);

        return Ok(settings);
    }
}
