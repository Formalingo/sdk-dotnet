using System;
using System.Text.Json;
using Formalingo.Sdk.Generated.Models;
using Microsoft.Kiota.Serialization.Json;
using Xunit;

namespace Formalingo.Sdk.Serialization.Tests;

public class RecipientDispatchSerializationTests {
    [Fact]
    public void Recipient_create_result_preserves_dispatch_correlation() {
        var dispatchId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var model = new RecipientCreateResult {
            DispatchId = dispatchId,
            Token = "one-time-token",
            Link = "https://www.formalingo.com/f/one-time-token",
            PlainPassword = "one-time-password",
        };

        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue<RecipientCreateResult>(null, model);
        using var json = JsonDocument.Parse(writer.GetSerializedContent());

        Assert.Equal(dispatchId, json.RootElement.GetProperty("dispatchId").GetGuid());
        Assert.Equal("one-time-token", json.RootElement.GetProperty("token").GetString());
        Assert.Equal(
            "https://www.formalingo.com/f/one-time-token",
            json.RootElement.GetProperty("link").GetString()
        );
        Assert.Equal("one-time-password", json.RootElement.GetProperty("plain_password").GetString());
        Assert.False(json.RootElement.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public void Document_submission_result_preserves_safe_dispatch_receipt() {
        var submissionId = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var dispatchId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var model = new CreateSubmissionResult {
            SubmissionId = submissionId,
            DispatchId = dispatchId,
            DispatchReused = true,
            LinksCreated = true,
        };

        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue<CreateSubmissionResult>(null, model);
        using var json = JsonDocument.Parse(writer.GetSerializedContent());

        Assert.Equal(submissionId, json.RootElement.GetProperty("submissionId").GetGuid());
        Assert.Equal(dispatchId, json.RootElement.GetProperty("dispatchId").GetGuid());
        Assert.True(json.RootElement.GetProperty("dispatchReused").GetBoolean());
        Assert.True(json.RootElement.GetProperty("linksCreated").GetBoolean());

        var signer = new CreateSubmissionSignerResult {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000043"),
            Label = "Buyer",
            Role = "buyer",
            Name = "Alice",
            Color = "#13A373",
            Order = 0,
            Link = "https://www.formalingo.com/d/one-time-token",
        };
        using var signerWriter = new JsonSerializationWriter();
        signerWriter.WriteObjectValue<CreateSubmissionSignerResult>(null, signer);
        using var signerJson = JsonDocument.Parse(signerWriter.GetSerializedContent());

        Assert.Equal(
            "https://www.formalingo.com/d/one-time-token",
            signerJson.RootElement.GetProperty("link").GetString()
        );
        Assert.False(signerJson.RootElement.TryGetProperty("token", out _));
        Assert.False(signerJson.RootElement.TryGetProperty("email", out _));
        Assert.False(signerJson.RootElement.TryGetProperty("phone", out _));
        Assert.False(signerJson.RootElement.TryGetProperty("passwordHash", out _));
    }
}
