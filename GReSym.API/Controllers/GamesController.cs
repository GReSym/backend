using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GReSym.API.Extensions;
using GReSym.Application.DTO.Games;
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

    public GamesController(IGamesService service)
    {
        _service = service;
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
    public async Task<IActionResult> GetPopularGames([FromQuery] GamesListRequestDto request)
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
}