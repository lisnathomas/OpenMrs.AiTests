using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace OpenMrs.Tests.Pages;

/// <summary>
/// Page Object for the OpenMRS 3 login screens.
///
/// OpenMRS logs in over three screens: username, then password (after clicking
/// Continue), then a location picker. The selectors below match the ones OpenMRS
/// uses in its own Playwright tests (openmrs-esm-core/e2e), and they are
/// "user-facing" locators: they find elements by label, role and text, the way a
/// person sees the page, so they survive most HTML changes.
/// </summary>
public sealed class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page) => _page = page;

    private ILocator UsernameInput => _page.GetByLabel(new Regex("username", RegexOptions.IgnoreCase));
    private ILocator ContinueButton => _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("continue", RegexOptions.IgnoreCase) });
    // ^ and $ make sure this matches the "Password" field, not the "Show password" button.
    private ILocator PasswordInput => _page.GetByLabel(new Regex("^password$", RegexOptions.IgnoreCase));
    private ILocator LogInButton => _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("log in", RegexOptions.IgnoreCase) });
    private ILocator ConfirmLocationButton => _page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("confirm", RegexOptions.IgnoreCase) });

    public ILocator ErrorMessage(string text) => _page.GetByText(text);

    /// <summary>Relative to the base URL, so this opens .../openmrs/spa/login.</summary>
    public async Task OpenAsync() => await _page.GotoAsync("spa/login");

    public async Task LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await ContinueButton.ClickAsync();
        // Playwright waits for the password field to become visible before typing.
        await PasswordInput.FillAsync(password);
        await LogInButton.ClickAsync();
    }

    public async Task ChooseLocationAsync(string location)
    {
        await _page.WaitForURLAsync(new Regex("/spa/login/location"));
        await _page.GetByText(new Regex(Regex.Escape(location), RegexOptions.IgnoreCase)).ClickAsync();
        await ConfirmLocationButton.ClickAsync();
    }
}
