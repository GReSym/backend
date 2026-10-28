using GReSym.Application.DTO.Evaluation;
using GReSym.Application.Interfaces;
using GReSym.Core.Common;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

/// <summary>Offline evaluation of the recommendations on collected Steam users (docs/evaluation.md).</summary>
[ApiController]
[Route("api/admin/evaluation")]
[Authorize(Roles = "Admin")]
public class EvaluationController : ControllerBase
{
    // A user's whole library is in the file: 200 users can take tens of MB
    private const long ImportSizeLimit = 200L * 1024 * 1024;

    private readonly IEvaluationService _evaluationService;

    public EvaluationController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    /// <summary>Imports the file of ml/scripts/collect_steam_users.py. Users already present are skipped.</summary>
    [HttpPost("steam-users/import")]
    [RequestSizeLimit(ImportSizeLimit)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ImportSteamUsersResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> ImportSteamUsers([FromBody] ImportSteamUsersRequestDto request)
    {
        try
        {
            var result = await _evaluationService.ImportSteamUsers(request);

            return Ok(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("steam-users")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SteamUsersSummaryDto), 200)]
    public async Task<IActionResult> GetSteamUsers()
    {
        return Ok(await _evaluationService.GetSteamUsersSummary());
    }

    /// <summary>Queues an evaluation run for the ML worker. Poll <c>GET runs/{id}</c> for the result.</summary>
    [HttpPost("runs")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StartEvaluationResponseDto), 202)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 503)]
    public async Task<IActionResult> StartRun([FromBody] StartEvaluationRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _evaluationService.StartRun(request, cancellationToken);

            return AcceptedAtAction(nameof(GetRun), new { id = result.RunId }, result);
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

    /// <summary>Runs, newest first, without their results.</summary>
    [HttpGet("runs")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EvaluationRunsListDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetRuns([FromQuery] int page = 1, [FromQuery] int limit = Constants.Api.DefaultPageSize)
    {
        try
        {
            return Ok(await _evaluationService.GetRuns(page, limit));
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("runs/{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EvaluationRunDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<IActionResult> GetRun(int id)
    {
        try
        {
            return Ok(await _evaluationService.GetRun(id));
        }
        catch (EvaluationRunNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
