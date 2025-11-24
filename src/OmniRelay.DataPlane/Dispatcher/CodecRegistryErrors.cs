using Hugo;

namespace OmniRelay.Dispatcher;

internal static class CodecRegistryErrors
{
    private const string LocalServiceRequiredCode = "dispatcher.codec.local_service_required";
    private const string ServiceRequiredCode = "dispatcher.codec.service_required";
    private const string ProcedureRequiredCode = "dispatcher.codec.procedure_required";
    private const string DuplicateRegistrationCode = "dispatcher.codec.duplicate";

    public static Error LocalServiceRequired() =>
        Error.From("Local service name cannot be null or whitespace.", LocalServiceRequiredCode);

    public static Error ServiceRequired() =>
        Error.From("Service identifier cannot be null or whitespace.", ServiceRequiredCode);

    public static Error ProcedureRequired() =>
        Error.From("Procedure name cannot be null or whitespace.", ProcedureRequiredCode);

    public static Error Duplicate(string scope, string service, string procedure, ProcedureKind kind) =>
        Error.From("Codec for the specified procedure is already registered.", DuplicateRegistrationCode)
            .WithMetadata("scope", scope)
            .WithMetadata("service", service)
            .WithMetadata("procedure", procedure)
            .WithMetadata("kind", kind.ToString());
}
