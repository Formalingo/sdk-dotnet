#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Formalingo.Sdk.Generated;
using Formalingo.Sdk.Generated.Api.V1.Forms.Item.Recipients.Bulk;
using Formalingo.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Serialization.Json;
using Xunit;

namespace Formalingo.Sdk.Serialization.Tests;

public sealed class DocumentSubmissionConvenienceTests
{
    [Fact]
    public async Task Emits_idempotency_metadata_and_returns_data_signers()
    {
        var signer = new CreateSubmissionSignerResult {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000043"),
            Label = "Buyer",
            Role = "buyer",
            Name = "Alice",
            Color = "#13A373",
            Order = 0,
            Link = "https://www.formalingo.com/d/one-time-token",
        };
        var response = new CreateSubmissionResponse {
            Success = true,
            Data = new CreateSubmissionResult {
                SubmissionId = Guid.Parse("00000000-0000-0000-0000-000000000041"),
                DispatchId = Guid.Parse("00000000-0000-0000-0000-000000000042"),
                DispatchReused = false,
                LinksCreated = true,
                Signers = [signer],
            },
        };
        var adapter = new CapturingRequestAdapter(response);
        var client = new FormalingoClient(adapter);
        var body = new CreateSubmissionBody {
            Signers = [
                new SignerInput {
                    Role = "buyer",
                    Name = "Alice",
                    Email = "alice@example.com",
                },
            ],
        };

        var submission = await FormalingoClientFactory.CreateDocumentSubmissionAsync(
            client,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            body,
            "document-create-1");

        var request = Assert.IsType<RequestInformation>(adapter.RequestInfo);
        Assert.Equal(Method.POST, request.HttpMethod);
        Assert.Equal(
            "https://example.test/api/v1/documents/00000000-0000-0000-0000-000000000001/submissions",
            request.URI.ToString());
        Assert.True(request.Headers.TryGetValue("Idempotency-Key", out var headerValues));
        Assert.Equal(["document-create-1"], headerValues);

        using var json = await JsonDocument.ParseAsync(request.Content);
        Assert.Equal("document", json.RootElement.GetProperty("deliveryFormat").GetString());
        var emittedSigner = json.RootElement.GetProperty("signers")[0];
        Assert.Equal("buyer", emittedSigner.GetProperty("role").GetString());
        Assert.Equal("Alice", emittedSigner.GetProperty("name").GetString());
        Assert.Equal("alice@example.com", emittedSigner.GetProperty("email").GetString());

        Assert.Equal(
            "https://www.formalingo.com/d/one-time-token",
            submission.Signers?[0].Link);
    }

    [Fact]
    public async Task Rejects_invalid_idempotency_metadata_before_sending()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            FormalingoClientFactory.CreateDocumentSubmissionAsync(
                null!,
                Guid.Empty,
                new CreateSubmissionBody(),
                "contains a space"));

        Assert.Contains("1-255 printable ASCII", error.Message);
    }

    [Fact]
    public async Task Bulk_recipients_emit_required_idempotency_metadata_and_return_data()
    {
        var response = new BulkPostResponse {
            Success = true,
            Data = [
                new RecipientBulkCreateResult {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000043"),
                    Label = "Alice",
                },
            ],
        };
        var adapter = new CapturingRequestAdapter(response);
        var client = new FormalingoClient(adapter);
        var body = new BulkPostRequestBody {
            ConfirmBulk = true,
            SendNotifications = false,
            Recipients = [
                new BulkPostRequestBody_recipients { Label = "Alice" },
            ],
        };

        var recipients = await FormalingoClientFactory.CreateBulkRecipientsAsync(
            client,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            body,
            "recipient-bulk-1");

        var request = Assert.IsType<RequestInformation>(adapter.RequestInfo);
        Assert.Equal(Method.POST, request.HttpMethod);
        Assert.Equal(
            "https://example.test/api/v1/forms/00000000-0000-0000-0000-000000000001/recipients/bulk",
            request.URI.ToString());
        Assert.True(request.Headers.TryGetValue("Idempotency-Key", out var headerValues));
        Assert.Equal(["recipient-bulk-1"], headerValues);
        Assert.Single(recipients);
        Assert.Equal("Alice", recipients[0].Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains a space")]
    public async Task Bulk_recipients_reject_invalid_idempotency_metadata(
        string idempotencyKey)
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            FormalingoClientFactory.CreateBulkRecipientsAsync(
                null!,
                Guid.Empty,
                new BulkPostRequestBody(),
                idempotencyKey));

        Assert.Contains("1-255 printable ASCII", error.Message);
    }

    [Fact]
    public async Task Bulk_recipients_reject_overlong_idempotency_metadata()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            FormalingoClientFactory.CreateBulkRecipientsAsync(
                null!,
                Guid.Empty,
                new BulkPostRequestBody(),
                new string('a', 256)));

        Assert.Contains("1-255 printable ASCII", error.Message);
    }

    private sealed class CapturingRequestAdapter(IParsable response) : IRequestAdapter
    {
        [AllowNull]
        public string BaseUrl { get; set; } = "https://example.test";

        public ISerializationWriterFactory SerializationWriterFactory { get; }
            = new JsonSerializationWriterFactory();

        public RequestInformation? RequestInfo { get; private set; }

        public Task<ModelType?> SendAsync<ModelType>(
            RequestInformation requestInfo,
            ParsableFactory<ModelType> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            where ModelType : IParsable
        {
            RequestInfo = requestInfo;
            return Task.FromResult((ModelType?)response);
        }

        public Task<IEnumerable<ModelType>?> SendCollectionAsync<ModelType>(
            RequestInformation requestInfo,
            ParsableFactory<ModelType> factory,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            where ModelType : IParsable
            => throw new NotSupportedException();

        public Task<ModelType?> SendPrimitiveAsync<ModelType>(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IEnumerable<ModelType>?> SendPrimitiveCollectionAsync<ModelType>(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SendNoContentAsync(
            RequestInformation requestInfo,
            Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<T?> ConvertToNativeRequestAsync<T>(
            RequestInformation requestInfo,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
        {
        }
    }
}
