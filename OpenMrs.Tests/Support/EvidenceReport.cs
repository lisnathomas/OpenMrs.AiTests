using System.Net;
using System.Text;

namespace OpenMrs.Tests.Support;

/// <summary>
/// Collects the result of every scenario and writes one HTML page per test run
/// (report.html in the run folder) with each scenario's status, video, trace,
/// screenshot and API request/response evidence.
/// </summary>
public static class EvidenceReport
{
    public sealed record ScenarioResult(
        string Feature,
        string Scenario,
        string[] Tags,
        bool Passed,
        string? Error,
        TimeSpan Duration,
        string? VideoFile,
        string? TraceFile,
        string? ScreenshotFile,
        IReadOnlyList<string> ApiFiles);

    private static readonly List<ScenarioResult> Results = new();
    private static readonly object Lock = new();

    public static void Add(ScenarioResult result)
    {
        lock (Lock)
        {
            Results.Add(result);
        }
    }

    public static string? Write()
    {
        List<ScenarioResult> results;
        lock (Lock)
        {
            results = Results.ToList();
        }
        if (results.Count == 0)
        {
            return null;
        }

        Directory.CreateDirectory(TestSettings.RunDir);
        var passed = results.Count(r => r.Passed);
        var html = new StringBuilder();

        html.Append($$"""
            <!doctype html>
            <html lang="en"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>OpenMRS test evidence</title>
            <style>
              body { font: 15px/1.5 "Segoe UI", Arial, sans-serif; margin: 0; background: #f4f6f8; color: #1f2933; }
              header { background: #0b3d4f; color: #fff; padding: 20px 28px; }
              header h1 { margin: 0 0 4px; font-size: 22px; }
              main { max-width: 1100px; margin: 0 auto; padding: 20px; }
              .summary { display: flex; gap: 12px; margin: 8px 0 20px; flex-wrap: wrap; }
              .pill { padding: 6px 12px; border-radius: 999px; background: #fff; border: 1px solid #d5dde3; }
              .card { background: #fff; border: 1px solid #d5dde3; border-left: 6px solid #2e7d32; border-radius: 6px; padding: 16px 18px; margin: 14px 0; }
              .card.fail { border-left-color: #c62828; }
              .card h3 { margin: 0 0 6px; font-size: 17px; }
              .meta { color: #52606d; font-size: 13px; }
              .status { font-weight: 700; color: #2e7d32; }
              .fail .status { color: #c62828; }
              pre { background: #0f172a; color: #e2e8f0; padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 12.5px; max-height: 420px; }
              video, img { max-width: 100%; border: 1px solid #d5dde3; border-radius: 6px; margin-top: 10px; }
              details { margin-top: 8px; }
              summary { cursor: pointer; font-weight: 600; }
              h2 { margin-top: 28px; color: #0b3d4f; }
              .error { background: #fdecea; color: #8a1c1c; padding: 10px; border-radius: 6px; white-space: pre-wrap; }
            </style></head><body>
            <header><h1>OpenMRS test evidence</h1>
            <div>Run {{Html(Path.GetFileName(TestSettings.RunDir))}} &middot; {{Html(TestSettings.BaseUrl)}}</div></header>
            <main>
            <div class="summary">
              <span class="pill">{{results.Count}} scenarios</span>
              <span class="pill">&#10003; {{passed}} passed</span>
              <span class="pill">&#10007; {{results.Count - passed}} failed</span>
            </div>
            """);

        foreach (var feature in results.GroupBy(r => r.Feature))
        {
            html.Append($"<h2>{Html(feature.Key)}</h2>");
            foreach (var r in feature)
            {
                html.Append($"""
                    <section class="card{(r.Passed ? "" : " fail")}">
                    <h3>{Html(r.Scenario)}</h3>
                    <div class="meta"><span class="status">{(r.Passed ? "PASSED" : "FAILED")}</span>
                    &middot; {r.Duration.TotalSeconds:0.0}s &middot; {Html(string.Join(" ", r.Tags.Select(t => "@" + t)))}</div>
                    """);

                if (!r.Passed && r.Error is not null)
                {
                    html.Append($"<div class=\"error\">{Html(r.Error)}</div>");
                }
                if (r.VideoFile is not null)
                {
                    html.Append($"<video controls preload=\"metadata\" src=\"{Rel(r.VideoFile)}\"></video>");
                }
                if (r.ScreenshotFile is not null)
                {
                    html.Append($"<details open><summary>Screenshot at failure</summary><img src=\"{Rel(r.ScreenshotFile)}\" alt=\"Screenshot at failure\"></details>");
                }
                if (r.TraceFile is not null)
                {
                    html.Append($"""
                        <details><summary>Step-by-step trace</summary>
                        <p>Open <a href="https://trace.playwright.dev" target="_blank" rel="noopener">trace.playwright.dev</a>
                        and drop in <a href="{Rel(r.TraceFile)}">{Html(Path.GetFileName(r.TraceFile))}</a>.
                        The trace opens in your browser and isn't uploaded anywhere.</p></details>
                        """);
                }
                foreach (var file in r.ApiFiles)
                {
                    html.Append($"""
                        <details><summary>{Html(Path.GetFileNameWithoutExtension(file))}</summary>
                        <pre>{Html(File.ReadAllText(file))}</pre></details>
                        """);
                }
                html.Append("</section>");
            }
        }

        html.Append("</main></body></html>");
        File.WriteAllText(TestSettings.ReportPath, html.ToString());
        return TestSettings.ReportPath;
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);

    private static string Rel(string file) =>
        Path.GetRelativePath(TestSettings.RunDir, file).Replace('\\', '/');
}
