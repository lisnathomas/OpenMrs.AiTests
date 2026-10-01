using System.Text.RegularExpressions;
using Reqnroll;
using OpenMrs.Tests.Pages;
using OpenMrs.Tests.Support;
using static Microsoft.Playwright.Assertions;

namespace OpenMrs.Tests.StepDefinitions;

/// <summary>
/// UI step definitions for logging in. Each [Given]/[When]/[Then] phrase links a line
/// in a .feature file to a method. FeatureBot reads these phrases and only lets the AI
/// use them, so every generated feature file can run.
/// </summary>
[Binding]
public sealed class LoginSteps
{
    private readonly BrowserSession _session;

    public LoginSteps(BrowserSession session) => _session = session;

    private LoginPage LoginPage => new(_session.Page);
    private HomePage HomePage => new(_session.Page);

    [Given("I am on the OpenMRS login page")]
    public async Task GivenIAmOnTheLoginPage() =>
        await LoginPage.OpenAsync();

    [When("I log in as the admin user")]
    public async Task WhenILogInAsTheAdminUser() =>
        await LoginPage.LoginAsync(TestSettings.Username, TestSettings.Password);

    [When("I log in with username {string} and password {string}")]
    public async Task WhenILogInWith(string username, string password) =>
        await LoginPage.LoginAsync(username, password);

    [When("I choose the {string} login location")]
    public async Task WhenIChooseTheLoginLocation(string location) =>
        await LoginPage.ChooseLocationAsync(location);

    [Then("I should see the OpenMRS home page")]
    public async Task ThenIShouldSeeTheHomePage() =>
        await Expect(_session.Page).ToHaveURLAsync(new Regex("/spa/home"));

    [Then("the top navigation should show the location {string}")]
    public async Task ThenTheTopNavigationShouldShowTheLocation(string location)
    {
        var locationText = HomePage.TopNavigation.GetByText(new Regex(Regex.Escape(location), RegexOptions.IgnoreCase));
        await Expect(locationText).ToBeVisibleAsync();
        await Highlight.CheckedElementAsync(locationText); // outline it in the video
    }

    [Then("I should still be on the login page")]
    public async Task ThenIShouldStillBeOnTheLoginPage() =>
        await Expect(_session.Page).ToHaveURLAsync(new Regex("/spa/login$"));

    [Then("I should see the login error {string}")]
    public async Task ThenIShouldSeeTheLoginError(string message)
    {
        var error = LoginPage.ErrorMessage(message);
        await Expect(error).ToBeVisibleAsync();
        await Highlight.CheckedElementAsync(error); // outline it in the video
    }
}
