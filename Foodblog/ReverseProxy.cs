using Microsoft.AspNetCore.HttpOverrides;

namespace Foodblog;

// Production runs behind nginx on the same host. Honor X-Forwarded-For/-Proto so the app sees the
// real client IP and https scheme. The defaults trust loopback proxies only, so a remote client
// cannot spoof these headers.
public static class ReverseProxy
{
    public static ForwardedHeadersOptions Options() => new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
}
