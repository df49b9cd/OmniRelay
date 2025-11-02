using System;
using System.Linq;
using Polymer.Core;
using Polymer.Transport.Grpc;
using Xunit;

namespace Polymer.Tests.Transport;

public class GrpcPeerChooserTests
{
    [Fact]
    public void RoundRobinChooser_CyclesAcrossPeers()
    {
        var peers = new[]
        {
            new Uri("http://127.0.0.1:1111"),
            new Uri("http://127.0.0.1:2222"),
            new Uri("http://127.0.0.1:3333")
        };

        var chooser = new RoundRobinGrpcPeerChooser();
        var meta = new RequestMeta("svc", "echo", transport: "grpc");

        var selections = Enumerable.Range(0, 6)
            .Select(_ => chooser.ChoosePeer(meta, peers))
            .ToArray();

        Assert.Equal(
            new[]
            {
                peers[0],
                peers[1],
                peers[2],
                peers[0],
                peers[1],
                peers[2]
            },
            selections);
    }

    [Fact]
    public void RoundRobinChooser_ThrowsOnEmptyPeers()
    {
        var chooser = new RoundRobinGrpcPeerChooser();
        var meta = new RequestMeta("svc", "echo", transport: "grpc");

        Assert.Throws<ArgumentException>(() => chooser.ChoosePeer(meta, Array.Empty<Uri>()));
    }
}
