using Sheridan.Aspire.Pulumi;
using Sheridan.Aspire.Pulumi.Azure;
using Sheridan.Aspire.Pulumi.Azure.EntraId;

var builder = DistributedApplication.CreateBuilder(args);

// The Pulumi stack that provisions the Azure resources and Entra ID app registrations below.
// `aspire publish` generates the Pulumi program; `aspire deploy` runs `pulumi up`.
builder.AddPulumiStack("Aspire-StarterApplication.1");

var env = builder.AddPulumiAzureContainerAppEnvironment("env");

// Entra ID app registration for the API. It exposes the "access_as_user" delegated scope.
// Pulumi creates the registrations when the stack is deployed, so they are hidden and not referenced in run mode.
// Locally, the services use the placeholder AzureAd settings in their appsettings.Development.json.
var apiEntra = builder.AddPulumiEntraId("apiservice-entra")
    .HideInRunMode()
    .WithScope("access_as_user", "Access the weather API on behalf of the signed-in user");

var apiService = builder.AddDotnetProject("apiservice", "../Aspire-StarterApplication.1.ApiService/Aspire-StarterApplication.1.ApiService.csproj")
    .WithComputeEnvironment(env)
    .WithHttpHealthCheck("/health");

if (!builder.ExecutionContext.IsRunMode)
{
    apiService.WithReference(apiEntra);
}

// Entra ID app registration for the web frontend. It signs users in and calls the API on their behalf.
// It also reads the signed-in user's profile photo from Microsoft Graph.
// When deployed it authenticates with a managed identity.
var webEntra = builder.AddPulumiEntraId("webfrontend-entra")
    .HideInRunMode()
    .WithApiPermissions(apiEntra, "access_as_user")
    .WithApiPermissions(EntraIdApi.MicrosoftGraph, EntraIdApiPermission.Delegated("User.Read"))
    .PublishWithFederatedManagedIdentityCredential(builder.CreateResourceBuilder<IPulumiManagedIdentityResource>(env.Resource));

var webFrontend = builder.AddDotnetProject("webfrontend", "../Aspire-StarterApplication.1.Web/Aspire-StarterApplication.1.Web.csproj")
    .WithComputeEnvironment(env)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

if (!builder.ExecutionContext.IsRunMode)
{
    webFrontend.WithReference(webEntra);
}

// Redirect URIs for the web frontend's sign-in. Pulumi registers them when the stack is deployed. Add the
// deployed frontend's address (https://<host>/signin-oidc) here once it is known.
webEntra.WithRedirectUris("https://localhost:7000/signin-oidc", "https://localhost:7000/signout-callback-oidc");

builder.Build().Run();
