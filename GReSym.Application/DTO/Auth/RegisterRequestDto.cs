using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Auth;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(64, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;
}