using Sheridan.Aspire.Pulumi;

var builder = DistributedApplication.CreateBuilder(args);

// The Pulumi stack that provisions everything below. `aspire publish` generates the Pulumi
// program; `aspire deploy` runs `pulumi up`. Requires the Pulumi CLI (`pulumi login`).
builder.AddPulumiStack("Aspire-StarterApplication.1");

var env = builder.AddPulumiAzureContainerAppEnvironment("env");

#if UseRedisCache
var cache = builder.AddPulumiAzureManagedRedis("cache")
    .RunAsContainer();

#endif
var apiService = builder.AddDotnetProject("apiservice", "../Aspire-StarterApplication.1.ApiService/Aspire-StarterApplication.1.ApiService.csproj")
    .WithComputeEnvironment(env)
    .WithHttpHealthCheck("/health");

builder.AddDotnetProject("webfrontend", "../Aspire-StarterApplication.1.Web/Aspire-StarterApplication.1.Web.csproj")
    .WithComputeEnvironment(env)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
