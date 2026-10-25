using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;

namespace Foodblog.Test;

public class ReverseProxyTests
{
    [Fact]
    public async Task BaseUrl_uses_the_forwarded_scheme_from_a_local_proxy()
    {
        var request = await ThroughReverseProxy(from: IPAddress.Loopback, forwardedProto: "https");

        Assert.Equal("https://foodblog.ch", request.BaseUrl);
    }

    [Fact]
    public async Task BaseUrl_ignores_a_forwarded_scheme_from_a_remote_client()
    {
        var request = await ThroughReverseProxy(from: IPAddress.Parse("203.0.113.7"), forwardedProto: "https");

        Assert.Equal("http://foodblog.ch", request.BaseUrl);
    }

    private static async Task<Request> ThroughReverseProxy(IPAddress from, string forwardedProto)
    {
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = from;
        http.Request.Method = "GET";
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("foodblog.ch");
        http.Request.Path = "/";
        http.Request.Headers["X-Forwarded-For"] = "198.51.100.1";
        http.Request.Headers["X-Forwarded-Proto"] = forwardedProto;

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Microsoft.Extensions.Options.Options.Create(ReverseProxy.Options()));
        await middleware.Invoke(http);

        return await HttpAdapter.ToRequest(http);
    }
}
