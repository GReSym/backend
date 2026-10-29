using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;

namespace GReSym.Application.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserSettingsService(
        IUserSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSettingsDto> GetSettings(int userId)
    {
        var settings = await _settingsRepository.GetAsync(userId) ?? new UserSettings { UserId = userId };

        return ToDto(settings);
    }

    public async Task<UserSettingsDto> UpdateSettings(int userId, UpdateUserSettingsRequestDto request)
    {
        var settings = await _settingsRepository.GetAsync(userId);

        if (settings == null)
        {
            settings = new UserSettings { UserId = userId };
            await _settingsRepository.AddAsync(settings);
        }

        if (request.Mainstream is { } mainstream)
            settings.Mainstream = mainstream;
        if (request.DislikeMode != null)
            settings.DislikeMode = DislikeModes.Parse(request.DislikeMode);
        if (request.DislikeStrength is { } strength)
            settings.DislikeStrength = strength;

        settings.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return ToDto(settings);
    }

    public async Task<RecommendationTuning> GetTuning(int userId)
    {
        var settings = await _settingsRepository.GetAsync(userId) ?? new UserSettings { UserId = userId };

        return new RecommendationTuning(settings.Mainstream, settings.DislikeMode, settings.DislikeStrength);
    }

    private static UserSettingsDto ToDto(UserSettings settings) => new()
    {
        Mainstream = settings.Mainstream,
        DislikeMode = settings.DislikeMode.ToName(),
        DislikeStrength = settings.DislikeStrength
    };
}
