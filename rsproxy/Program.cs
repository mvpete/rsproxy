
// Usage: rsproxy -Clusters clusterA=https://localhost:1337;clusterB=https://localhost:1338
// Usage: rsproxy -Target https://localhost:8080 -Cluster clusterA

using rsproxy;
using Yarp.ReverseProxy;

if (args.Length < 2)
{
    Console.Write("Bad usage.");
    return;
}

var proxyArgs = ArgParser.Parse<RsProxyArgs>(args);

// Send an http request to tell another instance to change destination
if (proxyArgs.Target != null && proxyArgs.Cluster != null)
{
    await new HttpClient().PutAsJsonAsync($"{proxyArgs.Target}/proxy/destination", new ClusterDto() { Name = proxyArgs.Cluster });
    return;
}

if (proxyArgs.Clusters == null)
{
    Console.WriteLine("Bad usage.");
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
var clusterDefs = ParseClusterArgs(proxyArgs.Clusters);
if (clusterDefs.Count() < 1)
{
    Console.WriteLine("At least 1 cluster must be specified.");
    return;
}
// Set the destination to the first cluster.
var destCluster = clusterDefs.ElementAt(0).Name;
if (string.IsNullOrEmpty(destCluster))
{
    Console.WriteLine("Cluster cannot be empty.");
    return;
}

builder.Services.AddReverseProxy().LoadFromMemory(destCluster, clusterDefs);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();

// Setup proxy control routing
var lookup = app.Services.GetRequiredService<IProxyStateLookup>();
app.MapGet("/proxy/destination", () => destCluster);
app.MapPut("/proxy/destination", (ClusterDto cluster, ILogger<ClusterDto> logger) =>
{
    if (!lookup.TryGetCluster(cluster.Name, out var _))
        return Results.NotFound("Cluster does not exist.");
    destCluster = cluster.Name;
    logger.LogInformation("Set cluster destination: '{DestCluster}'", destCluster);
    return Results.Ok();
});

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy(proxyPipeline =>
    {
        // Custom cluster selection
        proxyPipeline.Use((context, next) =>
        {
            if (lookup.TryGetCluster(destCluster, out var cluster))
            {
                context.ReassignProxyRequest(cluster);
            }

            return next();
        });
        proxyPipeline.UseSessionAffinity();
        proxyPipeline.UseLoadBalancing();
    });
});

app.Run();

// Simple function that splits the cluster args.
IEnumerable<ClusterDefinition> ParseClusterArgs(string clusters)
{
    string[] clusterArgs = clusters.Split(';');
    foreach (var clusterArg in clusterArgs)
    {
        string[] def = clusterArg.Split("=");
        if (def.Length != 2)
            continue;
        yield return new ClusterDefinition() { Name = def[0], Destination = def[1] };
    }
}

