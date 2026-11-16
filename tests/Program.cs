using System.Net;
using System.Reflection;
using System.Text;
using Formalingo.Sdk;
using Formalingo.Sdk.Generated;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;

const string apiKey = "factory-test-key";
const string baseUrl = "https://api.example.test";
var client = FormalingoClientFactory.CreateClient(apiKey, baseUrl);
using var handler = new RecordingHandler();
using var transport = new HttpClient(handler);
var adapter = ReplaceTransport(client, transport);

foreach (var url in new[] {
    "https://api.example.test/api/v1/forms",
    "https://API.EXAMPLE.TEST:443/api/v1/forms",
})
{
    var result = await client.Api.V1.Forms.WithUrl(url).GetAsync();
    Assert(result?.Success == true, "The same-origin factory request failed.");
    Assert(handler.Authorization == $"Bearer {apiKey}", "The same-origin request did not authenticate.");
}

foreach (var url in new[] {
    "https://other.example.test/api/v1/forms",
    "https://api.example.test:444/api/v1/forms",
    "http://api.example.test/api/v1/forms",
    "https://name:password@api.example.test/api/v1/forms",
})
{
    var callsBefore = handler.Calls;
    try
    {
        await client.Api.V1.Forms.WithUrl(url).GetAsync();
        throw new Exception("An off-origin SDK request was accepted.");
    }
    catch (InvalidOperationException error)
    {
        Assert(!error.Message.Contains(apiKey), "The rejection exposed the API key.");
    }
    Assert(handler.Calls == callsBefore, "An off-origin request reached transport.");
}

foreach (var invalidBase in new[] {
    "/relative", "ftp://api.example.test", "https://name:password@api.example.test", "https://api.example.test:invalid",
})
{
    try
    {
        FormalingoClientFactory.CreateClient(apiKey, invalidBase);
        throw new Exception("An invalid or credentialed API base URL was accepted.");
    }
    catch (ArgumentException error)
    {
        Assert(!error.Message.Contains(apiKey), "Base validation exposed the API key.");
    }
}

var localClient = FormalingoClientFactory.CreateClient(apiKey, "http://127.0.0.1:35817");
using var localHandler = new RecordingHandler();
using var localTransport = new HttpClient(localHandler);
ReplaceTransport(localClient, localTransport);
try
{
    await localClient.Api.V1.Forms.GetAsync();
    throw new Exception("The upstream HTTPS-only authentication guard was weakened.");
}
catch (ArgumentException)
{
    Assert(localHandler.Calls == 0, "An HTTP credential request reached transport.");
}

Console.WriteLine(".NET SDK factory origin regression passed: same origin authenticated; foreign host, port, scheme and credentials blocked before transport; HTTPS guard retained.");

static HttpClientRequestAdapter ReplaceTransport(FormalingoClient client, HttpClient transport)
{
    // Keep the real public factory's provider and generated request builders.
    // The locked Kiota adapter has no public transport setter; reflection is
    // confined to this test to avoid adding an SDK-only extension point.
    var adapterProperty = typeof(BaseRequestBuilder).GetProperty("RequestAdapter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new Exception("The locked Kiota request adapter test seam changed.");
    var adapter = adapterProperty.GetValue(client) as HttpClientRequestAdapter
        ?? throw new Exception("The factory returned an unexpected adapter.");
    var transportField = typeof(HttpClientRequestAdapter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(field => field.FieldType == typeof(HttpClient));
    transportField.SetValue(adapter, transport);
    return adapter;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class RecordingHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public string? Authorization { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Authorization = request.Headers.Authorization?.ToString();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true,\"data\":[]}", Encoding.UTF8, "application/json"),
        });
    }
}
