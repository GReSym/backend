using GReSym.API.Extensions;
using GReSym.Application.DTO.GameLists;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

/// <summary>
/// The user's planned / rejected games. Liking / disliking a recommendation fills them too; listed games aren't recommended
/// again and count as weak likes / dislikes.
/// </summary>
[ApiController]
[Route("api/users/me/game-lists")]
[Authorize]
public class GameListsController : ControllerBase
{
    private readonly IGameListsService _service;

    public GameListsController(IGameListsService service)
    {
        _service = service;
    }

    /// <summary>One list (status = planned / rejected), the most recently listed first.</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameListPageDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> List([FromQuery] GameListPageRequestDto request)
    {
        try
        {
            return Ok(await _service.List(User.GetUserId(), request));
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{gameId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameListEntryDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> Get(int gameId)
    {
        try
        {
            return Ok(await _service.Get(User.GetUserId(), gameId));
        }
        catch (GameListEntryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    /// <summary>Puts the game in a list (moves it if it's in the other one).</summary>
    [HttpPut("{gameId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameListEntryDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> Set(int gameId, [FromBody] SetGameListStatusRequestDto request)
    {
        try
        {
            return Ok(await _service.Set(User.GetUserId(), gameId, request.Status));
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{gameId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> Remove(int gameId)
    {
        try
        {
            await _service.Remove(User.GetUserId(), gameId);

            return NoContent();
        }
        catch (GameListEntryNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
