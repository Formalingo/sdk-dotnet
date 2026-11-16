using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Formalingo.Sdk.Generated;

namespace Formalingo.Sdk;

public static class FormalingoClientFactory
{
    public static FormalingoClient CreateClient(
        string apiKey,
        string baseUrl = "https://formalingo.com")
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(origin.UserInfo))
        {
            throw new ArgumentException("The API base URL must be an absolute HTTP(S) URL without credentials.", nameof(baseUrl));
        }

        var auth = new OriginBoundAuthenticationProvider(
            new ApiKeyAuthenticationProvider(
                $"Bearer {apiKey}",
                "Authorization",
                ApiKeyAuthenticationProvider.KeyLocation.Header),
            origin);

        var adapter = new HttpClientRequestAdapter(auth);
        adapter.BaseUrl = baseUrl;

        return new FormalingoClient(adapter);
    }

    private sealed class OriginBoundAuthenticationProvider(
        IAuthenticationProvider provider,
        Uri origin) : IAuthenticationProvider
    {
        public Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var target = request.URI;
            if (!target.IsAbsoluteUri
                || !string.IsNullOrEmpty(target.UserInfo)
                || !string.Equals(target.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(target.IdnHost, origin.IdnHost, StringComparison.OrdinalIgnoreCase)
                || target.Port != origin.Port)
            {
                throw new InvalidOperationException("The SDK request must stay on the configured API origin.");
            }

            return provider.AuthenticateRequestAsync(request, additionalAuthenticationContext, cancellationToken);
        }
    }
}
