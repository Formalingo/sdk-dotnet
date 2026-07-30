using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Formalingo.Sdk.Generated;
using Formalingo.Sdk.Generated.Api.V1.Forms.Item.Recipients.Bulk;
using Formalingo.Sdk.Generated.Models;

namespace Formalingo.Sdk;

public static class FormalingoClientFactory
{
    public static FormalingoClient CreateClient(
        string apiKey,
        string baseUrl = "https://app.formalingo.com")
    {
        var auth = new ApiKeyAuthenticationProvider(
            $"Bearer {apiKey}",
            "Authorization",
            ApiKeyAuthenticationProvider.KeyLocation.Header);

        var adapter = new HttpClientRequestAdapter(auth);
        adapter.BaseUrl = baseUrl;

        return new FormalingoClient(adapter);
    }

    /// <summary>
    /// Creates a retry-safe document signing submission and returns the response data.
    /// Reuse an idempotency key only when retrying the same logical request.
    /// </summary>
    public static async Task<CreateSubmissionResult> CreateDocumentSubmissionAsync(
        FormalingoClient client,
        Guid documentId,
        CreateSubmissionBody body,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        AssertIdempotencyKey(idempotencyKey);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(body);

        var response = await client.Api.V1.Documents[documentId].Submissions.PostAsync(
            body,
            request => request.Headers.Add("Idempotency-Key", idempotencyKey),
            cancellationToken).ConfigureAwait(false);

        return response?.Data
            ?? throw new InvalidOperationException(
                "Formalingo returned no document submission data");
    }

    /// <summary>
    /// Creates up to 100 recipients with caller-owned retry metadata.
    /// Reuse an idempotency key only when retrying the exact same request body.
    /// </summary>
    public static async Task<List<RecipientBulkCreateResult>> CreateBulkRecipientsAsync(
        FormalingoClient client,
        Guid formId,
        BulkPostRequestBody body,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        AssertIdempotencyKey(idempotencyKey);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(body);

        var response = await client.Api.V1.Forms[formId].Recipients.Bulk.PostAsync(
            body,
            request => request.Headers.Add("Idempotency-Key", idempotencyKey),
            cancellationToken).ConfigureAwait(false);

        return response?.Data
            ?? throw new InvalidOperationException(
                "Formalingo returned no bulk recipient data");
    }

    private static void AssertIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey)
            || idempotencyKey.Length > 255
            || idempotencyKey.Any(character => character is < '\x21' or > '\x7e'))
        {
            throw new ArgumentException(
                "idempotencyKey must contain 1-255 printable ASCII characters without spaces",
                nameof(idempotencyKey));
        }
    }
}
