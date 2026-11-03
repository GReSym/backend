namespace GReSym.Infrastructure.Media;

/// <summary>"Openinary" section of appsettings: the media store's management API (not the public media gateway).</summary>
public class OpeninarySettings
{
    /// <summary>Upload/storage API: <c>/api</c> of the full Openinary image (its nginx proxies it to the API).</summary>
    public string ApiUrl { get; set; } = "http://localhost:3000/api";
    /// <summary>Created in the Openinary dashboard (API Keys), stored in backend/config.json as openinary_api_key.</summary>
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
