using System.Collections.Immutable;

namespace OmniRelay.Security.Authorization;

/// <summary>Represents a set of role/cluster/principal requirements.</summary>
public sealed class MeshAuthorizationPolicy
{
    public MeshAuthorizationPolicy(
        string name,
        ImmutableHashSet<string> allowedRoles,
        ImmutableHashSet<string> allowedClusters,
        ImmutableDictionary<string, string> requiredLabels,
        ImmutableHashSet<string> principals)
    {
        Name = name;
        AllowedRoles = allowedRoles;
        AllowedClusters = allowedClusters;
        RequiredLabels = requiredLabels;
        Principals = principals;
    }

    public string Name { get; }

    public ImmutableHashSet<string> AllowedRoles { get; }

    public ImmutableHashSet<string> AllowedClusters { get; }

    public ImmutableDictionary<string, string> RequiredLabels { get; }

    public ImmutableHashSet<string> Principals { get; }
}
