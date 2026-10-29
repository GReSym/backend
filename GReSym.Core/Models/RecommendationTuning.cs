using GReSym.Core.Enums;

namespace GReSym.Core.Models;

/// <summary>The user's knobs for one recommendation request (stored settings + per-request overrides).</summary>
public record RecommendationTuning(int Mainstream, DislikeMode DislikeMode, int DislikeStrength);
