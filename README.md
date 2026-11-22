# Sheridan.Templates

A collection of project templates, starting with .NET.

## Templates

All templates provision Azure with [`Sheridan.Aspire.Pulumi`](https://github.com/two4suited/Sheridan.Aspire.Pulumi) instead of Bicep. They target net11.0 (required by those packages) and need the Pulumi CLI to publish or deploy. Each one's `aspire.config.json` registers a custom integration source so `aspire add` offers the packages:

```json
"integrationSources": {
  "sheridan": {
    "source": "https://api.nuget.org/v3/index.json",
    "patterns": ["Sheridan.Aspire.Pulumi.*"]
  }
}
```

- [`aspire-azure-pulumi-starter`](templates/aspire-azure-pulumi-starter): Blazor frontend and ASP.NET Core API on Azure Container Apps, with optional Azure Managed Redis.
- [`aspire-aks-pulumi-starter`](templates/aspire-aks-pulumi-starter): The same starter on Azure Kubernetes Service, with a VNet, Application Gateway for Containers and Let's Encrypt TLS. The Azure infrastructure is only declared when publishing or deploying.
- [`aspire-entra-pulumi-starter`](templates/aspire-entra-pulumi-starter): The Azure starter secured with Microsoft Entra ID; users sign in to the Blazor frontend, which calls the API with a JWT access token. Entra app registrations are provisioned by Pulumi on deploy, so local runs use placeholder `AzureAd` settings in each service's `appsettings.Development.json` (replace them to sign in locally).

Based on the templates in [Azure/aspire-templates](https://github.com/Azure/aspire-templates) (MIT).

## Testing

[`tests/Aspire.Templates.Tests`](tests/Aspire.Templates.Tests) verifies every template under [`templates`](templates). For each template, it creates an app with `aspire new --template-path`, starts it with `aspire run`, and checks that all resources reach a running and healthy state without logging errors.

Prerequisites: the [Aspire CLI](https://aspire.dev/get-started/install-cli/) on the `PATH` (or set `ASPIRE_CLI_PATH`) and the .NET 10 SDK.

```shell
dotnet test --project tests/Aspire.Templates.Tests
```

Set `ASPIRE_TEMPLATE_TESTS_STARTUP_TIMEOUT_SECONDS` to change how long resources may take to start (default: 300).
