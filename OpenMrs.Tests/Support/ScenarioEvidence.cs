namespace OpenMrs.Tests.Support;

/// <summary>
/// Everything saved as proof for the current scenario: video, trace, screenshot
/// and API request/response files. One per scenario, shared by hooks and steps.
/// </summary>
public sealed class ScenarioEvidence
{
    public DateTime StartedAt { get; set; } = DateTime.Now;

    /// <summary>A file-name-safe version of the scenario title, used to name every file.</summary>
    public string FileStem { get; set; } = "";

    public string? VideoFile { get; set; }
    public string? TraceFile { get; set; }
    public string? ScreenshotFile { get; set; }
    public List<string> ApiFiles { get; } = new();
}
