using GReSym.Application.DTO.Media;

namespace GReSym.Application.Interfaces;

/// <summary>Moves game images from the Steam CDN to the media store, one run at a time, in the background.</summary>
public interface IMediaMigrationService
{
    /// <exception cref="Core.Exceptions.DomainException">A run is already in progress.</exception>
    MediaMigrationStatusDto Start(StartMediaMigrationRequestDto request);

    MediaMigrationStatusDto GetStatus();

    /// <summary>Stops the running run after the games in progress. False if nothing is running.</summary>
    bool Cancel();

    Task<MediaStatsDto> GetStats(CancellationToken cancellationToken = default);
}
