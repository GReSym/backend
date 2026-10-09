using GReSym.API.Extensions;
using GReSym.Application.DTO.Recommendations;
using GReSym.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

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

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecommendationsResponseDto), 200)]
    public async Task<IActionResult> GetRecommendations([FromQuery] RecommendationsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _service.GetRecommendations(User.GetUserId(), request, cancellationToken);

        return Ok(result);
    }
}
