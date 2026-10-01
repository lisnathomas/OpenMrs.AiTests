using Microsoft.Playwright;

namespace OpenMrs.Tests.Support;

/// <summary>
/// Draws a pink outline around an element a test has just checked, so anyone watching
/// the video can see exactly what was verified.
/// </summary>
public static class Highlight
{
    public static async Task CheckedElementAsync(ILocator locator)
    {
        try
        {
            await locator.EvaluateAsync(
                "el => { el.style.outline = '3px solid #e5007d'; el.style.outlineOffset = '3px'; el.scrollIntoView({ block: 'center' }); }");
        }
        catch (PlaywrightException)
        {
            // Highlighting is only for the video; never fail a test because of it.
        }
    }
}
