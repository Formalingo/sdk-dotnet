# Formalingo .NET SDK

Official .NET SDK for the [Formalingo API](https://formalingo.com/docs), generated with [Microsoft Kiota](https://learn.microsoft.com/en-us/openapi/kiota/).

## Installation

```bash
dotnet add package Formalingo.Sdk
```

## Quick Start

```csharp
using Formalingo.Sdk;

var client = FormalingoClientFactory.CreateClient("af_live_YOUR_KEY");

// List forms
var forms = await client.Api.V1.Forms.GetAsync();

// Create a form
var form = await client.Api.V1.Forms.PostAsync(new() {
    Title = "Customer Satisfaction Survey",
    Description = "Help us improve our service.",
});

// Get a specific form
var details = await client.Api.V1.Forms["FORM_ID"].GetAsync();

// Delete a form
await client.Api.V1.Forms["FORM_ID"].DeleteAsync();
```

## Custom Base URL

```csharp
var client = FormalingoClientFactory.CreateClient(
    "af_live_YOUR_KEY",
    "http://localhost:3000"
);
```

## Bulk Create Recipients Safely

```csharp
using Formalingo.Sdk.Generated.Api.V1.Forms.Item.Recipients.Bulk;

var body = new BulkPostRequestBody {
    ConfirmBulk = true,
    Recipients = [
        new BulkPostRequestBody_recipients {
            Label = "Alice",
            Email = "alice@example.com",
        },
    ],
};
var recipients = await FormalingoClientFactory.CreateBulkRecipientsAsync(
    client,
    Guid.Parse("00000000-0000-0000-0000-000000000001"),
    body,
    "recipient-bulk-create-7f3f");
```

The required caller-owned key makes ambiguous retries safe. Reuse it only with the exact same serialized request body.
On `idempotency_request_in_progress`, retry the exact body with the same key. A different body returns `idempotency_key_conflict`; recipient erasure returns `idempotency_replay_unavailable`.

## Create a Document Submission

```csharp
using Formalingo.Sdk.Generated.Models;

var body = new CreateSubmissionBody {
    Signers = [
        new SignerInput {
            Role = "signer_1",
            Name = "Alice",
            Email = "alice@example.com",
        },
    ],
};
var submission = await FormalingoClientFactory.CreateDocumentSubmissionAsync(
    client,
    Guid.Parse("00000000-0000-0000-0000-000000000001"),
    body,
    "document-create-7f3f");

Console.WriteLine(submission.Signers?[0].Link);
```

## Documentation

- [.NET SDK Guide](https://formalingo.com/docs/sdks/dotnet)
- [API Reference](https://formalingo.com/docs/api-reference)

## License

MIT
