using GReSym.Application.DTO.Tags;
using GReSym.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GReSym.API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly IGamesService _service;

    public TagsController(IGamesService service)
    {
        _service = service;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TagsListResponseDto), 200)]
    public IActionResult SuggestTags([FromQuery] TagSuggestionsRequestDto request)
    {
        return Ok(_service.SuggestTags(request));
    }
}
