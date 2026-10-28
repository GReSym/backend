using GReSym.Application.DTO.Evaluation;

namespace GReSym.Application.Interfaces;

public interface IEvaluationService
{
    Task<ImportSteamUsersResponseDto> ImportSteamUsers(ImportSteamUsersRequestDto request);
    Task<SteamUsersSummaryDto> GetSteamUsersSummary();
    Task<StartEvaluationResponseDto> StartRun(StartEvaluationRequestDto request, CancellationToken cancellationToken = default);
    Task<EvaluationRunsListDto> GetRuns(int page, int limit);
    Task<EvaluationRunDto> GetRun(int id);
}
