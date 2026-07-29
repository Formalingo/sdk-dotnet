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
}
