using OmniRelay.Dispatcher.CodeGen;

namespace OmniRelay.Samples.ResourceLease.MeshDemo;

/// <summary>
/// Code-first dispatcher definition for the mesh demo. The source generator emits a factory method
/// that builds a Dispatcher with the configured service name without using reflection or configuration binding.
/// Add a partial method implementation `static partial void Configure(IServiceProvider services, DispatcherOptions options)`
/// in this class to programmatically wire transports, middleware, and codecs.
/// </summary>
[DispatcherDefinition("resourcelease-mesh-demo")]
public partial class MeshDemoDispatcher
{
    // Optional: implement the generated partial Configure method in another part to add transports/middleware.
}

