using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GReSym.API.Extensions;
using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Media;
using GReSym.Application.DTO.Settings;
using GReSym.Application.DTO.Tags;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IGamesService _gamesService;
    private readonly IAppSettingsService _settingsService;
    private readonly IMediaMigrationService _mediaMigration;

    public AdminController(IGamesService gamesService, IAppSettingsService settingsService,
        IMediaMigrationService mediaMigration)
    {
        _gamesService = gamesService;
        _settingsService = settingsService;
        _mediaMigration = mediaMigration;
    }

    [HttpGet("settings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AppSettingsDto), 200)]
    public async Task<IActionResult> GetSettings()
    {
        return Ok(await _settingsService.GetSettings());
    }

    [HttpPatch("settings")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AppSettingsDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> PatchSettings([FromBody] UpdateAppSettingsRequestDto request)
    {
        try
        {
            return Ok(await _settingsService.UpdateSettings(request));
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>How many headers / screenshots still point at the Steam CDN. Takes a second or two.</summary>
    [HttpGet("media/stats")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MediaStatsDto), 200)]
    public async Task<IActionResult> GetMediaStats(CancellationToken cancellationToken)
    {
        return Ok(await _mediaMigration.GetStats(cancellationToken));
    }

    /// <summary>
    /// Starts moving game images from the Steam CDN to Openinary in the background (one run at a time).
    /// Poll <c>GET media/migration</c> for progress.
    /// </summary>
    [HttpPost("media/migration")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MediaMigrationStatusDto), 202)]
    [ProducesResponseType(typeof(string), 409)]
    public IActionResult StartMediaMigration([FromBody] StartMediaMigrationRequestDto request)
    {
        try
        {
            return Accepted(_mediaMigration.Start(request));
        }
        catch (DomainException e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpGet("media/migration")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MediaMigrationStatusDto), 200)]
    public IActionResult GetMediaMigration()
    {
        return Ok(_mediaMigration.GetStatus());
    }

    /// <summary>Stops the running migration after the games in progress (their images are kept).</summary>
    [HttpDelete("media/migration")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 404)]
    public IActionResult CancelMediaMigration()
    {
        return _mediaMigration.Cancel() ? NoContent() : NotFound("No media migration is running.");
    }

    [HttpPatch("games/{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameInfoResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> PatchGame(int id, [FromBody] UpdateGameInfoRequestDto request)
    {
        try
        {
            var game = await _gamesService.UpdateGame(id, request);

            return Ok(game);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>Queues games for (re)vectorization by the ML worker. Processing is asynchronous.</summary>
    [HttpPost("games/vectorize")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(VectorizeGamesResponseDto), 202)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 503)]
    public async Task<IActionResult> VectorizeGames([FromBody] VectorizeGamesRequestDto request)
    {
        try
        {
            var result = await _gamesService.EnqueueVectorization(request);

            return Accepted(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
        catch (MessageQueueException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Message broker is unavailable.");
        }
    }

    /// <summary>
    /// Games whose vector is out of date (description changed, reviews/sentiment shifted, never vectorized, ...),
    /// best candidates first. Pick games and send them to <c>POST games/vectorize</c>. Takes a few seconds.
    /// </summary>
    /// <param name="reason">Only these reasons (repeatable): description, sentiment, reviews, growth, outdated, missing, deleted.</param>
    [HttpGet("vectorization/stale")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StaleGamesResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 503)]
    public async Task<IActionResult> GetStaleVectors(
        [FromQuery] string[]? reason,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _gamesService.GetStaleVectors(reason ?? [], limit, offset, cancellationToken);

            return Ok(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
        catch (MlServiceException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "ML service is unavailable.");
        }
    }

    [HttpPut("games/{gameId:int}/tags")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TagsListResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetGameTags(int gameId, [FromBody] TagsListDto request)
    {
        try
        {
            var tags = await _gamesService.UpdateTags(gameId, request);

            return Ok(tags);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("games/{gameId:int}/tags")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TagsListResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> PostGameTags(int gameId, [FromBody] TagsListDto request)
    {
        try
        {
            var tags = await _gamesService.AddTags(gameId, request);

            return Ok(tags);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("games/{gameId:int}/tags")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TagsListResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> DeleteGameTags(int gameId, [FromBody] TagsListDto request)
    {
        try
        {
            var tags = await _gamesService.RemoveTags(gameId, request);

            return Ok(tags);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }
}