using Microsoft.Playwright;

namespace OpenMrs.Tests.Support;

/// <summary>
/// One Playwright instance for the whole test run. UI tests use its browser;
/// API tests use its HTTP client (APIRequest). The browser only starts if a UI test needs it.
/// </summary>
public static class PlaywrightDriver
{
    public static IPlaywright Playwright { get; set; } = null!;
    private static IBrowser? _browser;

    public static async Task<IBrowser> GetBrowserAsync()
    {
        _browser ??= await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !TestSettings.Headed,
            SlowMo = TestSettings.SlowMoMs
        });
        return _browser;
    }

    public static async Task StopAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        Playwright?.Dispose();
    }
}
