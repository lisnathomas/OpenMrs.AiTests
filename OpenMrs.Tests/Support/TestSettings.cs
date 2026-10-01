using System.Text.Json;

namespace OpenMrs.Tests.Support;

/// <summary>
/// Test settings. Values come from testsettings.json (edit it in Visual Studio),
/// and an environment variable with the same meaning overrides the file
/// (useful later for CI, where the browser must run headless).
/// </summary>
public static class TestSettings
{
    private static readonly JsonElement? SettingsFile = LoadSettingsFile();

    // ---------------- OpenMRS ----------------

    /// <summary>OpenMRS address, without a trailing slash.</summary>
    public static string BaseUrl => GetString("OPENMRS_BASE_URL", "baseUrl", "http://localhost/openmrs").TrimEnd('/');

    /// <summary>The demo admin account. Local test environments only.</summary>
    public static string Username => GetString("OPENMRS_USERNAME", "username", "admin");
    public static string Password => GetString("OPENMRS_PASSWORD", "password", "Admin123");

    // ---------------- How the browser runs ----------------

    /// <summary>true = you see Chrome open. false = faster, invisible runs.</summary>
    public static bool Headed => GetBool("HEADED", "headed", false);

    /// <summary>Pause after every browser action, in milliseconds, so people can follow along.</summary>
    public static float SlowMoMs => GetFloat("SLOWMO", "slowMoMs", 0);

    /// <summary>Keep the final screen visible this long at the end of each UI scenario.</summary>
    public static float PauseAtEndMs => GetFloat("PAUSE_AT_END_MS", "pauseAtEndMs", 0);

    /// <summary>Show the current Gherkin step as a caption at the bottom of the page (and in the video).</summary>
    public static bool StepCaptions => GetBool("STEP_CAPTIONS", "stepCaptions", true);

    /// <summary>Record a Playwright trace (step-by-step replay) for every UI scenario.</summary>
    public static bool RecordTrace => GetBool("RECORD_TRACE", "recordTrace", true);

    // ---------------- Where evidence is saved ----------------

    public static string ArtifactsDir => Path.Combine(AppContext.BaseDirectory, "artifacts");

    /// <summary>Every test run gets its own folder, so earlier evidence is never overwritten.</summary>
    public static readonly string RunDir = Path.Combine(
        AppContext.BaseDirectory, "artifacts", $"run-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

    public static string VideoDir => Path.Combine(RunDir, "videos");
    public static string TraceDir => Path.Combine(RunDir, "traces");
    public static string ScreenshotDir => Path.Combine(RunDir, "screenshots");
    public static string ApiEvidenceDir => Path.Combine(RunDir, "api-evidence");
    public static string ReportPath => Path.Combine(RunDir, "report.html");
    public static string RawVideoDir => Path.Combine(ArtifactsDir, "raw-videos");

    // ---------------- Helpers ----------------

    private static JsonElement? LoadSettingsFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
        if (!System.IO.File.Exists(path))
        {
            return null;
        }
        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    private static string? FromFile(string name) =>
        SettingsFile is { } root && root.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            }
            : null;

    private static string GetString(string envName, string jsonName, string fallback) =>
        Environment.GetEnvironmentVariable(envName) ?? FromFile(jsonName) ?? fallback;

    private static bool GetBool(string envName, string jsonName, bool fallback) =>
        bool.TryParse(GetString(envName, jsonName, fallback.ToString()), out var value) ? value : fallback;

    private static float GetFloat(string envName, string jsonName, float fallback) =>
        float.TryParse(GetString(envName, jsonName, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value : fallback;
}
