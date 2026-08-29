using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Auth;

public class AuthResponseDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}