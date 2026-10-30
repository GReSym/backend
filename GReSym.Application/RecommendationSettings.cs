namespace GReSym.Application;

/// <summary>"Recommendations" section of appsettings.</summary>
public class RecommendationSettings
{
    /// <summary>
    /// Games kept by every recommendation except the user's newest one. Games with feedback are always kept.
    /// </summary>
    public int HistoryGamesLimit { get; set; } = 5;
}
