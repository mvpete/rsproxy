//License notice
//-------------------------------

//https://github.com/dotnet/runtime/blob/master/LICENSE.txt

//The MIT License (MIT)

//Copyright(c).NET Foundation and Contributors

//All rights reserved.

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using rsproxy;
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

internal static class InMemoryConfigProviderExtensions
{
    /// <summary>
    /// Adds an InMemoryConfigProvider
    /// </summary>
    public static IReverseProxyBuilder LoadFromMemory(this IReverseProxyBuilder builder, IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        builder.Services.AddSingleton(new InMemoryConfigProvider(routes, clusters));
        builder.Services.AddSingleton<IProxyConfigProvider>(s => s.GetRequiredService<InMemoryConfigProvider>());
        return builder;
    }

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