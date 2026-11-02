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
    private readonly IRatingsService _ratingsService;

    public UsersController(IUsersService service, IUserSettingsService settingsService, IRatingsService ratingsService)
    {
        _service = service;
        _settingsService = settingsService;
        _ratingsService = ratingsService;
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

    [HttpGet("me/stats")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserStatsDto), 200)]
    [Authorize]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _ratingsService.GetStats(User.GetUserId());

        return Ok(stats);
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
