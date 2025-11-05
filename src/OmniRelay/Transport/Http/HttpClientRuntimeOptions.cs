using System;
using System.Net;

namespace OmniRelay.Transport.Http;

public sealed class HttpClientRuntimeOptions
{
    public bool EnableHttp3 { get; init; }

    public Version? RequestVersion { get; init; }

    public HttpVersionPolicy? VersionPolicy { get; init; }
}
