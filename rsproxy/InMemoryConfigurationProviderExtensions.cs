using rsproxy;
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

internal static class InMemoryConfigProviderExtensions
{
    /// <summary>
    /// An additional extension method to configure from argument input.
    /// </summary>
    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder, string defaultCluster, IEnumerable<ClusterDefinition> clusterDefinitions)
    {
        return builder.LoadFromMemory(GetDefaultRouting(defaultCluster), GetClusterConfig(clusterDefinitions));
    }

    // Catch-all routing
    private static RouteConfig[] GetDefaultRouting(string defaultCluster)
    {
        return new[]
        {
            new RouteConfig()
            {
                RouteId="route1",
                ClusterId=defaultCluster,
                Match=new RouteMatch()
                {
                    Path="{**catch-all}"
                }
            }
        };
    }

    private static ClusterConfig[] GetClusterConfig(IEnumerable<ClusterDefinition> clusterDefinitions)
    {
        List<ClusterConfig> clusterConfigs = new List<ClusterConfig>();
        foreach (var def in clusterDefinitions)
        {
            if (string.IsNullOrEmpty(def.Name) || string.IsNullOrEmpty(def.Destination))
                continue;

            clusterConfigs.Add(new ClusterConfig()
            {
                ClusterId = def.Name,
                Destinations = new Dictionary<string, DestinationConfig>()
            {
                { "destination1", new DestinationConfig() { Address = def.Destination } }
            }
            });
        }
        return clusterConfigs.ToArray();
    }
}