using OmniRelay.Core.Leadership;
using Xunit;

namespace OmniRelay.Core.UnitTests.Leadership;

public sealed class LeadershipScopeTests
{
    [Fact]
    public void GlobalControl_HasExpectedValues()
    {
        var scope = LeadershipScope.GlobalControl;

        Assert.Equal("global-control", scope.ScopeId);
        Assert.Equal(LeadershipScopeKinds.Global, scope.Kind);
    }

    [Fact]
    public void CreateShard_GeneratesCorrectScopeId()
    {
        var scope = LeadershipScope.CreateShard("test-group", 5);

        Assert.Equal("shard:test-group:5", scope.ScopeId);
        Assert.Equal(LeadershipScopeKinds.Shard, scope.Kind);
    }

    [Fact]
    public void CreateCustom_UsesProvidedScopeId()
    {
        var scope = LeadershipScope.CreateCustom("custom-scope-id");

        Assert.Equal("custom-scope-id", scope.ScopeId);
        Assert.Equal(LeadershipScopeKinds.Custom, scope.Kind);
    }

    [Fact]
    public void Parse_ReturnsCorrectScope()
    {
        var descriptor = new LeadershipScopeDescriptor
        {
            ScopeId = "test-scope",
            Kind = LeadershipScopeKinds.Global
        };

        var scope = LeadershipScope.Parse(descriptor);

        Assert.Equal("test-scope", scope.ScopeId);
        Assert.Equal(LeadershipScopeKinds.Global, scope.Kind);
    }

    [Fact]
    public void Equality_ComparesScopeId()
    {
        var scope1 = new LeadershipScope("scope1", LeadershipScopeKinds.Global);
        var scope2 = new LeadershipScope("scope1", LeadershipScopeKinds.Global);
        var scope3 = new LeadershipScope("scope2", LeadershipScopeKinds.Global);

        Assert.Equal(scope1, scope2);
        Assert.NotEqual(scope1, scope3);
    }

    [Fact]
    public void ToString_ReturnsScopeId()
    {
        var scope = new LeadershipScope("my-scope", LeadershipScopeKinds.Custom);
        
        Assert.Equal("my-scope", scope.ToString());
    }
}
