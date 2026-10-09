namespace GReSym.Infrastructure.Ml;

/// <summary>"Ml" section of appsettings.</summary>
public class MlServiceSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 10;
}
