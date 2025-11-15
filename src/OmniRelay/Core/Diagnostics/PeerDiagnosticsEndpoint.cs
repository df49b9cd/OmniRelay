using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OmniRelay.Core.Gossip;

namespace OmniRelay.Core.Diagnostics;

/// <summary>Provides shared peer diagnostics endpoints for HTTP hosts.</summary>
internal static class PeerDiagnosticsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        System.ArgumentNullException.ThrowIfNull(app);

        Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(
            app,
            "/control/peers",
            (IMeshGossipAgent agent) => CreateResponse(agent));
        Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(
            app,
            "/omnirelay/control/peers",
            (IMeshGossipAgent agent) => CreateResponse(agent));
    }

    internal static IResult CreateResponse(IMeshGossipAgent agent)
    {
        if (agent is null)
        {
            return Results.Problem("Mesh gossip agent unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var snapshot = agent.Snapshot();
        var peers = snapshot.Members
            .Select(member => new PeerDiagnosticsPeer(
                member.NodeId,
                member.Status.ToString(),
                member.LastSeen,
                member.RoundTripTimeMs,
                new PeerDiagnosticsPeerMetadata(
                    member.Metadata.Role,
                    member.Metadata.ClusterId,
                    member.Metadata.Region,
                    member.Metadata.MeshVersion,
                    member.Metadata.Http3Support,
                    member.Metadata.Endpoint,
                    member.Metadata.MetadataVersion,
                    member.Metadata.Labels)))
            .ToArray();

        var response = new PeerDiagnosticsResponse(
            snapshot.SchemaVersion,
            snapshot.GeneratedAt,
            snapshot.LocalNodeId,
            peers);

        return Results.Json(response, DiagnosticsJsonContext.Default.PeerDiagnosticsResponse);
    }
}

internal sealed record PeerDiagnosticsResponse(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string LocalNodeId,
    IReadOnlyList<PeerDiagnosticsPeer> Peers);

internal sealed record PeerDiagnosticsPeer(
    string NodeId,
    string Status,
    DateTimeOffset? LastSeen,
    double? RttMs,
    PeerDiagnosticsPeerMetadata Metadata);

internal sealed record PeerDiagnosticsPeerMetadata(
    string Role,
    string ClusterId,
    string Region,
    string MeshVersion,
    bool Http3Support,
    string? Endpoint,
    long MetadataVersion,
    IReadOnlyDictionary<string, string> Labels);
