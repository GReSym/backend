using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Users;

public class UserInfoResponseDto
{
    [Required]
    public int Id { get; set; }
    [Required]
    [MaxLength(32)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public bool IsAdmin { get; set; }
}