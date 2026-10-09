using GReSym.API.Extensions;
using GReSym.Application.DTO.Ratings;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/users/me/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingsService _service;

    public RatingsController(IRatingsService service)
    {
        _service = service;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameRatingsListResponseDto), 200)]
    public async Task<IActionResult> GetRatings([FromQuery] GameRatingsListRequestDto request)
    {
        var result = await _service.GetRatings(User.GetUserId(), request);

        return Ok(result);
    }

    [HttpPut("{gameId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameRatingDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> RateGame(int gameId, [FromBody] RateGameRequestDto request)
    {
        try
        {
            var result = await _service.RateGame(User.GetUserId(), gameId, request);

            return Ok(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{gameId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> DeleteRating(int gameId)
    {
        try
        {
            await _service.DeleteRating(User.GetUserId(), gameId);

            return NoContent();
        }
        catch (RatingNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
