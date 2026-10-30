using GReSym.API.Extensions;
using GReSym.Application.DTO.Recommendations;
using GReSym.Application.Interfaces;
using GReSym.Core.Common;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

/// <summary>The user's recommendations: requested with POST, computed by the ML worker, kept as history.</summary>
[ApiController]
[Route("api/users/me/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationsService _service;

    public RecommendationsController(IRecommendationsService service)
    {
        _service = service;
    }

    /// <summary>Requests new recommendations. Poll <c>GET {id}</c> until the status is completed.</summary>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateRecommendationResponseDto), 202)]
    public async Task<IActionResult> Create([FromBody] RecommendationsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _service.Create(User.GetUserId(), request, cancellationToken);

        return AcceptedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>The user's recommendations, newest first, without games.</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecommendationHistoryDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int limit = Constants.Api.DefaultPageSize)
    {
        try
        {
            return Ok(await _service.GetHistory(User.GetUserId(), page, limit));
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecommendationDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            return Ok(await _service.Get(User.GetUserId(), id));
        }
        catch (RecommendationNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    /// <summary>The user's opinion about one recommended game (liked: true / false, null clears it).</summary>
    [HttpPut("{id:int}/games/{gameId:int}/feedback")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> SetFeedback(int id, int gameId, [FromBody] RecommendationFeedbackRequestDto request)
    {
        try
        {
            await _service.SetFeedback(User.GetUserId(), id, gameId, request.Liked);

            return NoContent();
        }
        catch (RecommendationNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
