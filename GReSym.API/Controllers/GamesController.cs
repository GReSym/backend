using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GReSym.API.Extensions;
using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Reviews;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IGamesService _service;
    private readonly IReviewsService _reviewsService;

    public GamesController(IGamesService service, IReviewsService reviewsService)
    {
        _service = service;
        _reviewsService = reviewsService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GamesListResponseDto), 200)]
    [ProducesResponseType(typeof(string), 401)]
    public async Task<IActionResult> GetGames([FromQuery] GamesListRequestDto request)
    {
        try
        {
            var result = await _service.GetGames(request);

            return Ok(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameInfoResponseDto), 200)]
    [ProducesResponseType(typeof(string), 401)]
    public async Task<IActionResult> GetGame(int id)
    {
        try
        {
            var result = await _service.GetGame(id);

            return Ok(result);
        }
        catch (GameNotFoundException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("popular")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GamesListResponseDto), 200)]
    [ProducesResponseType(typeof(string), 401)]
    public async Task<IActionResult> GetPopularGames([FromQuery] PopularGamesListRequestDto request)
    {
        try
        {
            var result = await _service.GetPopularGames(request);

            return Ok(result);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }


    [HttpGet("{gameId:int}/tags")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameInfoResponseDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    public async Task<IActionResult> GetGameTags(int gameId)
    {
        try
        {
            var game = await _service.GetGameTags(gameId);

            return Ok(game);
        }
        catch (DomainException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{gameId:int}/reviews")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GameReviewsListDto), 200)]
    public async Task<IActionResult> GetGameReviews(int gameId, [FromQuery] GameReviewsListRequestDto request)
    {
        var result = await _reviewsService.GetGameReviews(gameId, request);

        return Ok(result);
    }
}
