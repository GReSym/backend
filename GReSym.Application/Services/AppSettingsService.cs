using GReSym.Application.DTO.Settings;
using GReSym.Application.Interfaces;
using GReSym.Core.Entities.Settings;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace GReSym.Application.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MediaSettings _mediaSettings;

    public AppSettingsService(
        IAppSettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IOptions<MediaSettings> mediaSettings)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _mediaSettings = mediaSettings.Value;
    }

    public async Task<PublicConfigDto> GetPublicConfig()
    {
        return new PublicConfigDto { MediaBaseUrl = await GetMediaBaseUrl() };
    }

    public async Task<AppSettingsDto> GetSettings()
    {
        return new AppSettingsDto
        {
            MediaBaseUrl = await GetMediaBaseUrl(),
            DefaultMediaBaseUrl = NormalizeBaseUrl(_mediaSettings.DefaultBaseUrl)
        };
    }

    public async Task<AppSettingsDto> UpdateSettings(UpdateAppSettingsRequestDto request)
    {
        if (request.MediaBaseUrl != null)
        {
            var value = NormalizeBaseUrl(request.MediaBaseUrl);
            if (value.Length > 0 && !IsValidBaseUrl(value))
                throw new DomainException("Media base URL must be an http(s) URL or a path starting with \"/\".");

            var setting = await _settingsRepository.GetAsync(AppSetting.MediaBaseUrl);
            if (setting == null)
            {
                setting = new AppSetting { Key = AppSetting.MediaBaseUrl };
                await _settingsRepository.AddAsync(setting);
            }

            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetSettings();
    }

    /// <summary>The admin's value, the appsettings default while it's unset or cleared.</summary>
    private async Task<string> GetMediaBaseUrl()
    {
        var setting = await _settingsRepository.GetAsync(AppSetting.MediaBaseUrl);

        return setting is { Value.Length: > 0 } ? setting.Value : NormalizeBaseUrl(_mediaSettings.DefaultBaseUrl);
    }

    private static string NormalizeBaseUrl(string value) => value.Trim().TrimEnd('/');

    private static bool IsValidBaseUrl(string value)
    {
        if (value.StartsWith('/'))
            return !value.StartsWith("//") && Uri.IsWellFormedUriString(value, UriKind.Relative);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https"
               && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    }
}
