using GReSym.Application.DTO.Users;
using GReSym.Core.Models;

namespace GReSym.Application.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetSettings(int userId);
    Task<UserSettingsDto> UpdateSettings(int userId, UpdateUserSettingsRequestDto request);

    /// <summary>Stored settings (or defaults) for the recommendation engine.</summary>
    Task<RecommendationTuning> GetTuning(int userId);
}
