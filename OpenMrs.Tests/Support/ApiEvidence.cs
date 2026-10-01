using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenMrs.Tests.Support;

/// <summary>
/// Saves every API call as a readable JSON file: what was sent, and exactly what came back.
/// This is the API tests' equivalent of the UI tests' videos.
/// </summary>
public static class ApiEvidence
{
    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Save(
        ScenarioEvidence evidence,
        string step,
        string method,
        string url,
        IReadOnlyDictionary<string, string> requestHeaders,
        int status,
        string statusText,
        IReadOnlyDictionary<string, string> responseHeaders,
        string body)
    {
        var folder = Path.Combine(TestSettings.ApiEvidenceDir, evidence.FileStem);
        Directory.CreateDirectory(folder);

        // e.g. "02-GET-Patient-count-10.json"
        var pathAndQuery = new Uri(url).PathAndQuery;
        var r4 = pathAndQuery.IndexOf("/R4/", StringComparison.Ordinal);
        var resource = r4 >= 0 ? pathAndQuery[(r4 + 4)..] : pathAndQuery;
        var safeName = Regex.Replace(resource, @"[^A-Za-z0-9]+", "-").Trim('-');
        if (safeName.Length > 60) safeName = safeName[..60];
        var file = Path.Combine(folder, $"{evidence.ApiFiles.Count + 1:00}-{method}-{safeName}.json");

        object parsedBody;
        try
        {
            parsedBody = JsonDocument.Parse(body).RootElement.Clone();
        }
        catch (JsonException)
        {
            parsedBody = body; // e.g. the plain error page OpenMRS sends with a 401
        }

        var record = new
        {
            step,
            timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            request = new { method, url, headers = requestHeaders },
            response = new
            {
                status,
                statusText,
                contentType = responseHeaders.TryGetValue("content-type", out var type) ? type : "",
                body = parsedBody
            }
        };

        File.WriteAllText(file, JsonSerializer.Serialize(record, Pretty));
        evidence.ApiFiles.Add(file);
        return file;
    }
}
