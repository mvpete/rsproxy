Rail-Switch Proxy
===

Rail-Switch Proxy or RSProxy is a simple reverse proxy used to deterministically control
destination routing. This can be useful for A/B testing or any type of testing 
where deterministic routing is required.

RSProxy allows for run-time destination switching, similar to a switch on a rail track.
Using any HTTP client, you can change the target destination of the proceeding requests
by simply issuing a request to change the destination.

Example usage
---

`rsproxy --urls "https://localhost:8080" -Clusters clusterA=https://localhost:1337;clusterB=https://localhost:1338;`

In this example we're using the --urls parameter of ASPNETCore to set the binding address of the proxy to https://localhost:8080,
and adding two cluster destinations clusterA at https://localhost:1337 and clusterB at https://localhost:1338. The `Clusters` 
argument supports any length of semi-colon delimeted key-value pairs of destinations.

Any HTTP client can be used to send a request like the following:

```
PUT https://localhost:8080/proxy/destination
Content-Type: application/json
{
  "Name":"clusterA"
}
```

A 200 OK response indicates the proxy has been switched.

Alternatively, you can use an instance of `rsproxy` itself to make the change. This is useful
specifically when scripting.

`rsproxy -Target https://localhost:8080 -Cluster clusterA`

This will launch a new instance of rsproxy which sends the request to the target to change 
its destination.
