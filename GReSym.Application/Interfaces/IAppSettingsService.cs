using GReSym.Application.DTO.Settings;

namespace GReSym.Application.Interfaces;

public interface IAppSettingsService
{
    Task<PublicConfigDto> GetPublicConfig();
    Task<AppSettingsDto> GetSettings();
    Task<AppSettingsDto> UpdateSettings(UpdateAppSettingsRequestDto request);
}
