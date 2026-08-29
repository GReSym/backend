using System.Security.Authentication;
using GReSym.Application.DTO.Auth;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(string), 401)] 
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try {
            var result = await _service.LoginAsync(request);

            return Ok(result);
        }
        catch (InvalidCredentialException e) 
        {
            return Unauthorized(e.Message);
        }
    }

    [HttpPost("register")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)] 
    [ProducesResponseType(typeof(string), 401)] 
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try {
            var result = await _service.RegisterAsync(request);

            return Ok(result);
        }
        catch (EmailOccupiedException e) 
        {
            return BadRequest(e.Message);
        }
        catch (InvalidCredentialException e) 
        {
            return Unauthorized(e.Message);
        }
    }
}