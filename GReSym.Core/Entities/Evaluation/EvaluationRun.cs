using GReSym.Core.Entities.Base;

namespace GReSym.Core.Entities.Evaluation;

/// <summary>One offline evaluation of the recommendations, computed by the ML worker.</summary>
public class EvaluationRun : BaseEntity
{
    /// <summary>One of <see cref="EvaluationStatus"/>.</summary>
    public string Status { get; set; } = EvaluationStatus.Queued;
    /// <summary>JSON (camelCase), read by the ML worker.</summary>
    public string Parameters { get; set; } = "{}";
    /// <summary>JSON report from the ML worker; null until completed.</summary>
    public string? Results { get; set; }
    public string? Error { get; set; }
    public int? UsersEvaluated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}

/// <summary>Values of <see cref="EvaluationRun.Status"/>; the ML worker reports the same strings.</summary>
public static class EvaluationStatus
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
