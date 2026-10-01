using Microsoft.Playwright;

namespace OpenMrs.Tests.Pages;

/// <summary>Page Object for what a clinician sees after logging in.</summary>
public sealed class HomePage
{
    private readonly IPage _page;

    public HomePage(IPage page) => _page = page;

    /// <summary>The top navigation bar (an ARIA "banner" named OpenMRS).</summary>
    public ILocator TopNavigation => _page.GetByRole(AriaRole.Banner, new() { Name = "OpenMRS" });

    public ILocator MyAccountButton => _page.GetByRole(AriaRole.Button, new() { Name = "My Account" });
}
