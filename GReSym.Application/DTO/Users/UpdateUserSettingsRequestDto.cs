using System.ComponentModel.DataAnnotations;
using GReSym.Application.Validation;

namespace GReSym.Application.DTO.Users;

/// <summary>Partial update: omitted fields keep their current value.</summary>
public class UpdateUserSettingsRequestDto
{
    [Range(0, 100)]
    public int? Mainstream { get; set; }

    /// <summary>ignore | penalize_closer | penalize_similar | hide_similar.</summary>
    [DislikeModeName]
    public string? DislikeMode { get; set; }

    [Range(0, 100)]
    public int? DislikeStrength { get; set; }
}
