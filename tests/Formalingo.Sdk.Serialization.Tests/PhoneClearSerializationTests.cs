using System.Text;
using System.Text.Json;
using System;
using Xunit;
using Formalingo.Sdk.Generated.Models;
using Microsoft.Kiota.Serialization.Json;

namespace Formalingo.Sdk.Serialization.Tests;

public class PhoneClearSerializationTests {
    private static JsonDocument Serialize<T>(T model) where T : Microsoft.Kiota.Abstractions.Serialization.IParsable {
        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue<T>(null, model);
        return JsonDocument.Parse(writer.GetSerializedContent());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Recipient_clear_phone_is_presence_sensitive(bool clear) {
        using var json = Serialize(new UpdateRecipientBody { ClearPhone = clear ? true : null });
        Assert.Equal(clear, json.RootElement.TryGetProperty("clearPhone", out var value));
        if (clear) Assert.True(value.GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Signer_clear_phone_is_presence_sensitive(bool clear) {
        using var json = Serialize(new UpdateSignerBody { ClearPhone = clear ? true : null });
        Assert.Equal(clear, json.RootElement.TryGetProperty("clearPhone", out var value));
        if (clear) Assert.True(value.GetBoolean());
    }
}
