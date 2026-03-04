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

## Documentation

- [.NET SDK Guide](https://formalingo.com/docs/sdks/dotnet)
- [API Reference](https://formalingo.com/docs/api-reference)

## License

MIT
