using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Formalingo.Sdk.Generated;

namespace Formalingo.Sdk;

public static class FormalingoClientFactory
{
    public static FormalingoClient CreateClient(
        string apiKey,
        string baseUrl = "https://formalingo.com")
    {
        var auth = new ApiKeyAuthenticationProvider(
            $"Bearer {apiKey}",
            "Authorization",
            ApiKeyAuthenticationProvider.KeyLocation.Header);

        var adapter = new HttpClientRequestAdapter(auth);
        adapter.BaseUrl = baseUrl;

        return new FormalingoClient(adapter);
    }
}
