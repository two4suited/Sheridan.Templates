using Sheridan.Aspire.Pulumi;
using Sheridan.Aspire.Pulumi.Azure.Kubernetes;
using Sheridan.Aspire.Pulumi.Azure.Network;

var builder = DistributedApplication.CreateBuilder(args);

// The Pulumi stack that provisions the Azure resources below. `aspire publish` generates the
// Pulumi program; `aspire deploy` runs `pulumi up`. Requires the Pulumi CLI (`pulumi login`).
builder.AddPulumiStack("Aspire-StarterApplication.1");

#if UseRedisCache
var cache = builder.AddPulumiAzureManagedRedis("cache")
    .RunAsContainer();

#endif
var apiService = builder.AddDotnetProject("apiservice", "../Aspire-StarterApplication.1.ApiService/Aspire-StarterApplication.1.ApiService.csproj")
    .WithHttpHealthCheck("/health");

var webfrontend = builder.AddDotnetProject("webfrontend", "../Aspire-StarterApplication.1.Web/Aspire-StarterApplication.1.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithReference(apiService)
    .WaitFor(apiService);

// The Azure infrastructure below is only provisioned when publishing or deploying; local runs
// use the projects directly and need no Azure resources, Pulumi CLI, or ACME contact email.
if (!builder.ExecutionContext.IsRunMode)
{
    var vnet = builder.AddPulumiVirtualNetwork("vnet", "10.100.0.0/16");
    var aksSubnet = vnet.AddSubnet("aks-nodes", "10.100.0.0/22");
    var albSubnet = vnet.AddSubnet("alb-public", "10.100.4.0/24");

    var nodeVmSize = builder.Configuration["Azure:AKS:VmSize"]
        ?? throw new InvalidOperationException("Azure:AKS:VmSize must specify an AKS node VM size.");
    var aks = builder.AddPulumiKubernetes("aks")
        .WithSubnet(aksSubnet)
        .WithSystemNodePool(nodeVmSize);
    aks.AddNodePool("workload", nodeVmSize, minCount: 1, maxCount: 3);

    var publicLb = aks.AddLoadBalancer("public", albSubnet);

    var acmeEmail = builder.AddParameter("acme-email");
    var acmeServer = builder.Configuration["Parameters:acme-server"]
        ?? throw new InvalidOperationException("Parameters:acme-server must specify an ACME server URL.");
    var certManager = aks.AddCertManager("cert-manager");
    var letsencrypt = certManager.AddIssuer("letsencrypt-prod")
        .WithAcmeServer(acmeServer, acmeEmail)
        .WithHttp01Solver();

    aks.AddGateway("web")
        .WithLoadBalancer(publicLb)
        .WithRoute("/", webfrontend.GetEndpoint("http"))
        .WithTls(letsencrypt);
}

builder.Build().Run();
