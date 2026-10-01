using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using OpenMrs.Tests.Support;

namespace OpenMrs.Tests.StepDefinitions;

/// <summary>
/// API step definitions for the OpenMRS FHIR R4 API (/ws/fhir2/R4).
/// Playwright isn't only for browsers: its APIRequest client sends HTTP requests,
/// much like Postman, but from code that runs in the same test suite as the UI tests.
/// </summary>
[Binding]
public sealed class FhirPatientSteps
{
    private const string FhirBase = "ws/fhir2/R4/";
    private readonly ApiSession _api;
    private readonly ScenarioContext _scenarioContext;
    private readonly ScenarioEvidence _evidence;
    private readonly IReqnrollOutputHelper _output;

    public FhirPatientSteps(ApiSession api, ScenarioContext scenarioContext,
        ScenarioEvidence evidence, IReqnrollOutputHelper output)
    {
        _api = api;
        _scenarioContext = scenarioContext;
        _evidence = evidence;
        _output = output;
    }

    // ---------------- Who is calling the API ----------------

    [Given("I am an authenticated FHIR API client")]
    public Task GivenIAmAnAuthenticatedClient() => CreateClientAsync(TestSettings.Username, TestSettings.Password);

    [Given("I am an anonymous FHIR API client")]
    public Task GivenIAmAnAnonymousClient() => CreateClientAsync(null, null);

    [Given("I am a FHIR API client with username {string} and password {string}")]
    public Task GivenIAmAClientWith(string username, string password) => CreateClientAsync(username, password);

    // ---------------- Requests ----------------

    [When("I request the FHIR capability statement")]
    public Task WhenIRequestTheCapabilityStatement() => GetAsync("metadata");

    [When("I search for patients")]
    public Task WhenISearchForPatients() => GetAsync("Patient?_count=10");

    [Given("I have searched for patients")]
    public async Task GivenIHaveSearchedForPatients()
    {
        await WhenISearchForPatients();
        Assert.That(_api.LastStatus, Is.EqualTo(200), $"Patient search failed: {Short(_api.LastBody)}");

        var patients = PatientsInBundle(_api.LastJson).ToList();
        Assert.That(patients, Is.Not.Empty,
            "No patients found. Register at least one patient in OpenMRS, then run the tests again.");
        _api.FirstSearchResult = patients[0];
    }

    [When("I read the first patient from the search results by id")]
    public Task WhenIReadTheFirstPatientById() => GetAsync($"Patient/{FirstPatientId}");

    [When("I search for patients by the family name of the first search result")]
    public Task WhenISearchByFamilyName() =>
        GetAsync($"Patient?family={Uri.EscapeDataString(FamilyName(FirstPatient))}&_count=100");

    [When("I request the patient with id {string}")]
    public Task WhenIRequestThePatientWithId(string id) => GetAsync($"Patient/{Uri.EscapeDataString(id)}");

    // ---------------- Checks ----------------

    [Then("the response status should be {int}")]
    public void ThenTheResponseStatusShouldBe(int expected) =>
        Assert.That(_api.LastStatus, Is.EqualTo(expected), $"Response body: {Short(_api.LastBody)}");

    [Then("the response should be a FHIR {string} resource")]
    public void ThenTheResponseShouldBeAFhirResource(string resourceType) =>
        Assert.That(_api.LastJson.GetProperty("resourceType").GetString(), Is.EqualTo(resourceType));

    [Then("the capability statement should declare FHIR version {string}")]
    public void ThenTheCapabilityStatementShouldDeclareVersion(string version) =>
        Assert.That(_api.LastJson.GetProperty("fhirVersion").GetString(), Is.EqualTo(version));

    [Then("every patient in the bundle should have an id, a name and a gender")]
    public void ThenEveryPatientShouldHaveIdNameAndGender()
    {
        var patients = PatientsInBundle(_api.LastJson).ToList();
        Assert.That(patients, Is.Not.Empty, "The search returned no patients.");

        // Check every patient and report all problems at once, instead of stopping at the first one.
        using (Assert.EnterMultipleScope())
        {
            foreach (var patient in patients)
            {
                var id = Text(patient, "id");
                Assert.That(id, Is.Not.Empty, "A patient has no id.");
                Assert.That(FamilyName(patient), Is.Not.Empty, $"Patient {id} has no family name.");
                Assert.That(Text(patient, "gender"), Is.Not.Empty, $"Patient {id} has no gender.");
            }
        }
    }

    [Then("the patient should match the first search result")]
    public void ThenThePatientShouldMatchTheFirstSearchResult()
    {
        var expected = FirstPatient;
        var actual = _api.LastJson;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Text(actual, "resourceType"), Is.EqualTo("Patient"));
            Assert.That(Text(actual, "id"), Is.EqualTo(Text(expected, "id")), "id");
            Assert.That(FamilyName(actual), Is.EqualTo(FamilyName(expected)), "family name");
            Assert.That(Text(actual, "gender"), Is.EqualTo(Text(expected, "gender")), "gender");
            Assert.That(Text(actual, "birthDate"), Is.EqualTo(Text(expected, "birthDate")), "birth date");
        }
    }

    [Then("the first search result should be in the results")]
    public void ThenTheFirstSearchResultShouldBeInTheResults()
    {
        var ids = PatientsInBundle(_api.LastJson).Select(p => Text(p, "id")).ToList();
        Assert.That(ids, Does.Contain(FirstPatientId),
            $"Searching by family name '{FamilyName(FirstPatient)}' didn't return patient {FirstPatientId}.");
    }

    // ---------------- Helpers ----------------

    private async Task CreateClientAsync(string? username, string? password)
    {
        var headers = new Dictionary<string, string> { ["Accept"] = "application/fhir+json" };
        _api.RequestHeaders["Accept"] = "application/fhir+json";
        if (username is not null)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            headers["Authorization"] = $"Basic {token}";
            // The evidence file shows who called the API, never the password.
            _api.RequestHeaders["Authorization"] = $"Basic ******** (user: {username})";
        }
        else
        {
            _api.RequestHeaders["Authorization"] = "(none: anonymous request)";
        }

        _api.Client = await PlaywrightDriver.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = TestSettings.BaseUrl + "/",
            ExtraHTTPHeaders = headers
        });
    }

    private async Task GetAsync(string path)
    {
        Assert.That(_api.Client, Is.Not.Null, "Start the scenario with a 'Given I am ... FHIR API client' step.");
        var response = await _api.Client!.GetAsync(FhirBase + path);
        _api.LastStatus = response.Status;
        _api.LastBody = await response.TextAsync();

        // Evidence: save the full request and response, and log one line in Test Explorer's output.
        var step = _scenarioContext.StepContext.StepInfo;
        var file = ApiEvidence.Save(_evidence, $"{step.StepDefinitionType} {step.Text}", "GET", response.Url,
            _api.RequestHeaders, response.Status, response.StatusText, response.Headers, _api.LastBody);
        _output.WriteLine($"GET {response.Url} -> {response.Status} {response.StatusText}");
        _output.WriteLine($"Evidence: {file}");
    }

    private JsonElement FirstPatient =>
        _api.FirstSearchResult ?? throw new InvalidOperationException("Use 'Given I have searched for patients' first.");

    private string FirstPatientId => Text(FirstPatient, "id");

    private static IEnumerable<JsonElement> PatientsInBundle(JsonElement bundle) =>
        bundle.TryGetProperty("entry", out var entries)
            ? entries.EnumerateArray()
                .Select(e => e.GetProperty("resource"))
                .Where(r => Text(r, "resourceType") == "Patient")
            : Enumerable.Empty<JsonElement>();

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static string FamilyName(JsonElement patient) =>
        patient.TryGetProperty("name", out var names) && names.GetArrayLength() > 0
            ? Text(names[0], "family")
            : "";

    private static string Short(string text) => text.Length > 500 ? text[..500] + "..." : text;
}
