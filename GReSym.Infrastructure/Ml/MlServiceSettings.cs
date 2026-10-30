namespace GReSym.Infrastructure.Ml;

/// <summary>"Ml" section of appsettings.</summary>
public class MlServiceSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    /// <summary>For the stale-vectors scan, which reads every game.</summary>
    public int ScanTimeoutSeconds { get; set; } = 120;
    /// <summary>For game search. The first description search (exact=false) loads the model on the ML side.</summary>
    public int SearchTimeoutSeconds { get; set; } = 60;
}
