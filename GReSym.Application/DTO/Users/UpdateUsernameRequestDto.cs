using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Users;

public class UpdateUsernameRequestDto
{
    [Required]
    [StringLength(32, MinimumLength = 4)]
    public string Username { get; set; } = string.Empty;
}