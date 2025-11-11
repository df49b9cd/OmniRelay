using OmniRelay.Core.Leadership;
using Xunit;

namespace OmniRelay.Core.UnitTests.Leadership;

public sealed class LeadershipOptionsTests
{
    [Fact]
    public void DefaultOptions_HasExpectedValues()
    {
        var options = new LeadershipOptions();

        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromSeconds(8), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(3), options.RenewalLeadTime);
        Assert.Equal(TimeSpan.FromMilliseconds(750), options.EvaluationInterval);
        Assert.Equal(TimeSpan.FromSeconds(5), options.MaxElectionWindow);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.ElectionBackoff);
        Assert.Null(options.NodeId);
        Assert.Empty(options.Scopes);
        Assert.Empty(options.Shards);
    }

    [Fact]
    public void Scopes_CanBeModified()
    {
        var options = new LeadershipOptions();
        options.Scopes.Add(new LeadershipScopeDescriptor
        {
            ScopeId = "scope1",
            Kind = LeadershipScopeKinds.Global
        });
        options.Scopes.Add(new LeadershipScopeDescriptor
        {
            ScopeId = "scope2",
            Kind = LeadershipScopeKinds.Shard
        });

        Assert.Equal(2, options.Scopes.Count);
        Assert.Equal("scope1", options.Scopes[0].ScopeId);
        Assert.Equal("scope2", options.Scopes[1].ScopeId);
    }

    [Fact]
    public void Shards_CanBeModified()
    {
        var options = new LeadershipOptions();
        options.Shards.Add(new LeadershipShardScopeOptions
        {
            Name = "shard-group-1",
            Count = 10
        });

        Assert.Single(options.Shards);
        Assert.Equal("shard-group-1", options.Shards[0].Name);
        Assert.Equal(10, options.Shards[0].Count);
    }
}
