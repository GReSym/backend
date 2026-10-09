using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GReSym.API.Extensions;
using GReSym.Application.DTO.Games;
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

    public AdminController(IGamesService gamesService)
    {
        _gamesService = gamesService;
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