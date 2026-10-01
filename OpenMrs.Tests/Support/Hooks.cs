using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;

namespace OpenMrs.Tests.Support;

/// <summary>
/// Code that runs around the test run, each scenario and each step.
///
/// Every scenario:  gets a start time and a file name for its evidence.
/// @ui scenarios:   a fresh browser context, video, trace, step captions,
///                  a pass/fail banner and a pause at the end so the result is visible.
/// @api scenarios:  their HTTP client is cleaned up (evidence is saved by the steps).
/// End of the run:  report.html with every scenario's evidence in one page.
/// </summary>
[Binding]
public sealed class Hooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;
    private readonly BrowserSession _browser;
    private readonly ApiSession _api;
    private readonly ScenarioEvidence _evidence;

    public Hooks(ScenarioContext scenarioContext, FeatureContext featureContext,
        BrowserSession browser, ApiSession api, ScenarioEvidence evidence)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
        _browser = browser;
        _api = api;
        _evidence = evidence;
    }

    // ======================= Test run =======================

    [BeforeTestRun]
    public static async Task StartPlaywright()
    {
        // Downloads Chromium the first time; does nothing if it is already installed.
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright could not install Chromium (exit code {exitCode}).");
        }

        // OpenMRS is a large single-page app; give screens a little longer to appear.
        Assertions.SetDefaultExpectTimeout(15_000);

        PlaywrightDriver.Playwright = await Playwright.CreateAsync();
        Directory.CreateDirectory(TestSettings.RawVideoDir);
        Directory.CreateDirectory(TestSettings.RunDir);
    }

    [AfterTestRun]
    public static async Task FinishRun()
    {
        var report = EvidenceReport.Write();
        if (report is not null)
        {
            Console.WriteLine($"Evidence report: {report}");
        }
        await PlaywrightDriver.StopAsync();
    }

    // ======================= Every scenario =======================

    [BeforeScenario(Order = 0)]
    public void StartEvidence()
    {
        _evidence.StartedAt = DateTime.Now;
        _evidence.FileStem = $"{SafeFileName(_scenarioContext.ScenarioInfo.Title)}-{DateTime.Now:HHmmss}";
    }

    // Runs after the @ui/@api clean-up hooks below (higher Order runs later).
    [AfterScenario(Order = 20_000)]
    public void RecordResult()
    {
        var error = _scenarioContext.TestError;
        EvidenceReport.Add(new EvidenceReport.ScenarioResult(
            Feature: _featureContext.FeatureInfo.Title,
            Scenario: _scenarioContext.ScenarioInfo.Title,
            Tags: _scenarioContext.ScenarioInfo.CombinedTags,
            Passed: error is null && _scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK,
            Error: error?.Message,
            Duration: DateTime.Now - _evidence.StartedAt,
            VideoFile: _evidence.VideoFile,
            TraceFile: _evidence.TraceFile,
            ScreenshotFile: _evidence.ScreenshotFile,
            ApiFiles: _evidence.ApiFiles.ToList()));
    }

    // ======================= @ui scenarios =======================

    [BeforeScenario("ui")]
    public async Task OpenBrowserTab()
    {
        var browser = await PlaywrightDriver.GetBrowserAsync();
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            // The trailing slash matters: "spa/login" then resolves to .../openmrs/spa/login
            BaseURL = TestSettings.BaseUrl + "/",
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            RecordVideoDir = TestSettings.RawVideoDir,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        });

        if (TestSettings.StepCaptions)
        {
            // Pages ask the test for the current caption after every page load.
            await context.ExposeFunctionAsync("__qaGetCaption", () => _browser.CurrentCaption);
            await context.AddInitScriptAsync(Captions.InitScript);
        }

        if (TestSettings.RecordTrace)
        {
            await context.Tracing.StartAsync(new TracingStartOptions
            {
                Title = _scenarioContext.ScenarioInfo.Title,
                Screenshots = true,
                Snapshots = true
            });
        }

        _browser.Context = context;
        _browser.Page = await context.NewPageAsync();
    }

    [BeforeStep("ui")]
    public async Task ShowStepCaption()
    {
        if (!TestSettings.StepCaptions || _browser.Page is null)
        {
            return;
        }

        var step = _scenarioContext.StepContext.StepInfo;
        _browser.CurrentCaption = $"{_scenarioContext.ScenarioInfo.Title}   \u203A   {step.StepDefinitionType} {step.Text}";
        await TrySetCaptionAsync(_browser.CurrentCaption, Captions.StepColor);
    }

    [AfterScenario("ui")]
    public async Task CloseBrowserTab()
    {
        var page = _browser.Page;
        if (page is null)
        {
            return; // the browser never opened, so there is nothing to save
        }

        var title = _scenarioContext.ScenarioInfo.Title;
        var error = _scenarioContext.TestError;

        // Show the result on screen so the end of the video says PASSED or FAILED.
        if (TestSettings.StepCaptions)
        {
            var banner = error is null
                ? $"\u2713 PASSED   {title}"
                : $"\u2717 FAILED   {title}:  {FirstLine(error.Message)}";
            _browser.CurrentCaption = banner;
            await TrySetCaptionAsync(banner, error is null ? Captions.PassColor : Captions.FailColor);
        }

        if (TestSettings.PauseAtEndMs > 0)
        {
            await page.WaitForTimeoutAsync(TestSettings.PauseAtEndMs);
        }

        if (error is not null)
        {
            Directory.CreateDirectory(TestSettings.ScreenshotDir);
            _evidence.ScreenshotFile = Path.Combine(TestSettings.ScreenshotDir, $"{_evidence.FileStem}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = _evidence.ScreenshotFile, FullPage = true });
        }

        if (TestSettings.RecordTrace)
        {
            Directory.CreateDirectory(TestSettings.TraceDir);
            _evidence.TraceFile = Path.Combine(TestSettings.TraceDir, $"{_evidence.FileStem}.zip");
            await _browser.Context.Tracing.StopAsync(new TracingStopOptions { Path = _evidence.TraceFile });
        }

        // The video is finished when the context closes; then save it with a readable name.
        await _browser.Context.CloseAsync();
        if (page.Video is not null)
        {
            Directory.CreateDirectory(TestSettings.VideoDir);
            _evidence.VideoFile = Path.Combine(TestSettings.VideoDir, $"{_evidence.FileStem}.webm");
            await page.Video.SaveAsAsync(_evidence.VideoFile);
            await page.Video.DeleteAsync();
        }
    }

    // ======================= @api scenarios =======================

    [AfterScenario("api")]
    public async Task CloseApiClient()
    {
        if (_api.Client is not null)
        {
            await _api.Client.DisposeAsync();
        }
    }

    // ======================= Helpers =======================

    private async Task TrySetCaptionAsync(string text, string color)
    {
        try
        {
            await _browser.Page.EvaluateAsync(Captions.SetCaptionScript, new[] { text, color });
        }
        catch (PlaywrightException)
        {
            // The page may be mid-navigation; the caption is redrawn when the next page loads.
        }
    }

    private static string FirstLine(string text) => text.Split('\n')[0].Trim();

    private static string SafeFileName(string title) =>
        Regex.Replace(title, @"[^A-Za-z0-9]+", "-").Trim('-');
}
