using System.Text.Json;

namespace GReSym.Application.DTO.Evaluation;

public class EvaluationRunDto
{
    public int Id { get; set; }

    /// <summary>queued, running, completed or failed.</summary>
    public string Status { get; set; } = string.Empty;

    public JsonElement Parameters { get; set; }

    public int? UsersEvaluated { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>The ML worker's report (only for a single run): metrics per strategy and K, per-user hits.</summary>
    public JsonElement? Results { get; set; }
}

public class EvaluationRunsListDto
{
    public int Count { get; set; }

    public List<EvaluationRunDto> Runs { get; set; } = [];
}

public class StartEvaluationResponseDto
{
    public int RunId { get; set; }
}
