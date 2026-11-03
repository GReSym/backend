using GReSym.Application.DTO.Settings;
using GReSym.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/config")]
[AllowAnonymous]
public class ConfigController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;

    public ConfigController(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>Site configuration for clients (media base URL, ...). Loaded once at startup.</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PublicConfigDto), 200)]
    public async Task<IActionResult> GetConfig()
    {
        return Ok(await _settingsService.GetPublicConfig());
    }
}
