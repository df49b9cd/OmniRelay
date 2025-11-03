using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polymer.Configuration;
using Xunit;
using PolymerDispatcher = Polymer.Dispatcher.Dispatcher;

namespace Polymer.Tests.Configuration;

public class PolymerConfigurationTests
{
    [Fact]
    public void AddPolymerDispatcher_BuildsDispatcherFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["polymer:service"] = "gateway",
                ["polymer:inbounds:http:0:urls:0"] = "http://127.0.0.1:8080",
                ["polymer:outbounds:keyvalue:unary:http:0:key"] = "primary",
                ["polymer:outbounds:keyvalue:unary:http:0:url"] = "http://127.0.0.1:8081",
                ["polymer:outbounds:keyvalue:oneway:http:0:key"] = "primary",
                ["polymer:outbounds:keyvalue:oneway:http:0:url"] = "http://127.0.0.1:8081",
                ["polymer:logging:level"] = "Warning",
                ["polymer:logging:overrides:Polymer.Transport.Http"] = "Trace"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPolymerDispatcher(configuration.GetSection("polymer"));

        using var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<PolymerDispatcher>();
        Assert.Equal("gateway", dispatcher.ServiceName);

        var clientConfig = dispatcher.ClientConfig("keyvalue");
        Assert.True(clientConfig.TryGetUnary("primary", out var unary));
        Assert.NotNull(unary);
        Assert.True(clientConfig.TryGetOneway("primary", out var oneway));
        Assert.NotNull(oneway);

        var loggerOptions = provider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value;
        Assert.Equal(LogLevel.Warning, loggerOptions.MinLevel);
        Assert.Contains(
            loggerOptions.Rules,
            rule => string.Equals(rule.CategoryName, "Polymer.Transport.Http", StringComparison.Ordinal) &&
                    rule.LogLevel == LogLevel.Trace);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.Single(hostedServices);
    }

    [Fact]
    public void AddPolymerDispatcher_MissingServiceThrows()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        Assert.Throws<PolymerConfigurationException>(
            () => services.AddPolymerDispatcher(configuration.GetSection("polymer")));
    }

    [Fact]
    public void AddPolymerDispatcher_InvalidPeerChooserThrows()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["polymer:service"] = "edge",
                ["polymer:outbounds:inventory:stream:grpc:0:addresses:0"] = "http://127.0.0.1:9090",
                ["polymer:outbounds:inventory:stream:grpc:0:peerChooser"] = "random-weighted"
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPolymerDispatcher(configuration.GetSection("polymer"));

        using var provider = services.BuildServiceProvider();
        Assert.Throws<PolymerConfigurationException>(() => provider.GetRequiredService<PolymerDispatcher>());
    }
}
