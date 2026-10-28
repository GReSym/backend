using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Evaluation;

/// <summary>Parameters of an evaluation run; omitted values take the defaults. See docs/evaluation.md.</summary>
public class StartEvaluationRequestDto
{
    /// <summary>Cut-offs K for the metrics (1–100, up to 10 values). Default 5, 10, 20.</summary>
    [MaxLength(10)]
    public List<int>? Ks { get; set; }

    /// <summary>Hidden games per user. Default 3.</summary>
    [Range(1, 20)]
    public int? TestSize { get; set; }

    /// <summary>Liked games that must remain after hiding, else the user is skipped. Default 5.</summary>
    [Range(1, 100)]
    public int? MinTrainGames { get; set; }

    /// <summary>Playtime from which an unreviewed game counts. Default 60.</summary>
    [Range(0, 1_000_000)]
    public int? MinPlaytimeMinutes { get; set; }

    /// <summary>"top" (most liked, default) or "random-liked".</summary>
    public string? TestSelection { get; set; }

    /// <summary>Random seed (random-liked selection, random baseline). Default 42.</summary>
    public int? Seed { get; set; }

    /// <summary>β of the hybrid strategies (similarity + β·popularity), each in (0, 10], up to 10 values. Default 0.05, 0.1, 0.2, 0.4.</summary>
    [MaxLength(10)]
    public List<double>? HybridBetas { get; set; }

    /// <summary>Mean-centered vectors for the vector strategies. Default false.</summary>
    public bool? Center { get; set; }
}
