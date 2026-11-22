# Aspire AKS starter (Pulumi)

A Blazor frontend and ASP.NET Core weather API with service discovery, health checks,
OpenTelemetry, and optional Azure Managed Redis output caching. Local development works
without Azure credentials. Infrastructure is provisioned with Pulumi through
[`Sheridan.Aspire.Pulumi`](https://github.com/two4suited/Sheridan.Aspire.Pulumi) instead of Bicep.

## Create and run

Install the .NET 11 SDK and the Aspire CLI, then from a checkout of this repository:

```bash
aspire new --template-path ./templates/aspire-aks-pulumi-starter --name MyAksApp --output ./MyAksApp
cd MyAksApp
aspire run
```

The generated `aspire.config.json` registers an `integrationSources` entry with the
`Sheridan.Aspire.Pulumi.*` pattern so `aspire add` offers those packages.

## Deployment model

The AppHost declares a Pulumi stack (`AddPulumiStack`) and provisions a VNet with AKS node and
Application Gateway for Containers subnets, an AKS cluster with system and workload node pools,
a gateway routing `/` to the frontend, and cert-manager with a Let's Encrypt HTTP-01 issuer.

- `aspire publish` generates the Pulumi program; `aspire deploy` runs `pulumi up`; `aspire destroy` runs `pulumi destroy`.
- Requires the Pulumi CLI (`pulumi login`), an Azure subscription, and the AGC preview feature registrations
  (`ManagedGatewayAPIPreview`, `ApplicationLoadBalancerPreview`).
- Node VM size comes from `Azure:AKS:VmSize` and the ACME server from `Parameters:acme-server` in the AppHost's `appsettings.json`.
  Set `Parameters__acme_email` to a real contact address before deploying.
- Deploying creates billable Azure resources.

See the [Sheridan.Aspire.Pulumi Kubernetes package](https://github.com/two4suited/Sheridan.Aspire.Pulumi/tree/main/src/Sheridan.Aspire.Pulumi.Azure.Kubernetes) for details and current limitations.
