using Polymer.Transport.Http.Middleware;

namespace Polymer.Transport.Http;

internal interface IHttpOutboundMiddlewareSink
{
    void Attach(string service, HttpOutboundMiddlewareRegistry registry);
}
