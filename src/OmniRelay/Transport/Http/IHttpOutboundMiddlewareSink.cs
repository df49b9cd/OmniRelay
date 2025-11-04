using OmniRelay.Transport.Http.Middleware;

namespace OmniRelay.Transport.Http;

internal interface IHttpOutboundMiddlewareSink
{
    void Attach(string service, HttpOutboundMiddlewareRegistry registry);
}
