﻿//License notice
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

using Microsoft.Extensions.Primitives;

namespace Yarp.ReverseProxy.Configuration;

/// <summary>
/// Provides an implementation of IProxyConfigProvider to support config being generated by code.
/// </summary>
internal sealed class InMemoryConfigProvider : IProxyConfigProvider
{
    // Marked as volatile so that updates are atomic
    private volatile InMemoryConfig _config;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public InMemoryConfigProvider(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        _config = new InMemoryConfig(routes, clusters);
    }

    /// <summary>
    /// Implementation of the IProxyConfigProvider.GetConfig method to supply the current snapshot of configuration
    /// </summary>
    /// <returns>An immutable snapshot of the current configuration state</returns>
    public IProxyConfig GetConfig() => _config;

    /// <summary>
    /// Swaps the config state with a new snapshot of the configuration, then signals that the old one is outdated.
    /// </summary>
    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        var newConfig = new InMemoryConfig(routes, clusters);
        var oldConfig = Interlocked.Exchange(ref _config, newConfig);
        oldConfig.SignalChange();
    }

    /// <summary>
    /// Implementation of IProxyConfig which is a snapshot of the current config state. The data for this class should be immutable.
    /// </summary>
    private class InMemoryConfig : IProxyConfig
    {
        // Used to implement the change token for the state
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public InMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        /// <summary>
        /// A snapshot of the list of routes for the proxy
        /// </summary>
        public IReadOnlyList<RouteConfig> Routes { get; }

        /// <summary>
        /// A snapshot of the list of Clusters which are collections of interchangable destination endpoints
        /// </summary>
        public IReadOnlyList<ClusterConfig> Clusters { get; }

        /// <summary>
        /// Fired to indicate the the proxy state has changed, and that this snapshot is now stale
        /// </summary>
        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            _cts.Cancel();
        }
    }
}