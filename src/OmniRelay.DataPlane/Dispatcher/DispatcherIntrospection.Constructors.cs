using System.Collections.Immutable;
using OmniRelay.Core;

namespace OmniRelay.Dispatcher;

public partial record DispatcherIntrospection
{
    // Backward-compatible constructor used by existing tests/tools.
    public DispatcherIntrospection(
        string service,
        DispatcherStatus status,
        ProcedureGroups procedures,
        ImmutableArray<LifecycleComponentDescriptor> components,
        ImmutableArray<OutboundDescriptor> outbounds,
        MiddlewareSummary middleware)
        : this(service, status, procedures, components, outbounds, middleware, DeploymentMode.InProc, ImmutableArray<string>.Empty)
    {
    }
}
