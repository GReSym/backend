namespace GReSym.Application.DTO.Games;

public class UpdateGameInfoRequestDto
{
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    // public string? HeaderImageUrl { get; set; }
    public List<string>? Tags { get; set; }
    // public List<string>? Screenshots { get; set; }
    // public string? SteamAppId { get; set; }

}