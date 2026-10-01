using System.Text.Json;
using Microsoft.Playwright;

namespace OpenMrs.Tests.Support;

/// <summary>
/// State for the current API scenario: the HTTP client and the last response.
/// Like BrowserSession, Reqnroll creates one per scenario.
/// </summary>
public sealed class ApiSession
{
    public IAPIRequestContext? Client { get; set; }
    public int LastStatus { get; set; }
    public string LastBody { get; set; } = "";

    /// <summary>The headers this client sends, with the password masked, for the evidence files.</summary>
    public Dictionary<string, string> RequestHeaders { get; } = new();

    /// <summary>The first patient returned by "I have searched for patients", kept for later comparisons.</summary>
    public JsonElement? FirstSearchResult { get; set; }

    /// <summary>The last response body parsed as JSON.</summary>
    public JsonElement LastJson => JsonDocument.Parse(LastBody).RootElement.Clone();
}
