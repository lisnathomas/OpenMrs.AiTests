namespace OpenMrs.Tests.Support;

/// <summary>
/// Draws the current Gherkin step as a caption bar at the bottom of the page, so the
/// recorded video explains itself ("When I log in as the admin user"...).
///
/// The caption lives inside a CLOSED shadow root. Playwright's locators can't see into
/// closed shadow roots, so a caption that says "Invalid username or password" can never
/// be mistaken by a test for the real error message on the page.
/// </summary>
public static class Captions
{
    /// <summary>Runs in every page the browser opens, before the page's own scripts.</summary>
    public const string InitScript = """
        (() => {
          let box = null;
          const set = (text, color) => {
            if (!text || !document.documentElement) return;
            if (!box) {
              const host = document.createElement('div');
              host.style.cssText = 'position:fixed;left:0;right:0;bottom:0;z-index:2147483647;pointer-events:none;';
              const root = host.attachShadow({ mode: 'closed' });
              box = document.createElement('div');
              box.style.cssText = 'padding:10px 18px;color:#fff;font:600 16px/1.4 "Segoe UI",Arial,sans-serif;pointer-events:none;';
              root.appendChild(box);
              document.documentElement.appendChild(host);
            }
            box.style.background = color || 'rgba(17,24,39,.85)';
            box.textContent = text;
          };
          window.__qaSetCaption = set;
          // After a page load, ask the test for the current caption (exposed as __qaGetCaption).
          const pull = () => {
            if (window.__qaGetCaption) window.__qaGetCaption().then(t => set(t)).catch(() => {});
          };
          if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', pull);
          else pull();
        })();
        """;

    public const string SetCaptionScript = "([text, color]) => window.__qaSetCaption && window.__qaSetCaption(text, color)";

    public const string StepColor = "rgba(17,24,39,.85)";
    public const string PassColor = "rgba(22,101,52,.92)";
    public const string FailColor = "rgba(153,27,27,.92)";
}
