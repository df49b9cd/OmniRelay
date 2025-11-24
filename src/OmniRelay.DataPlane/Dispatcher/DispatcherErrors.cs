using Hugo;

namespace OmniRelay.Dispatcher;

/// <summary>Shared error factories for dispatcher configuration and registration.</summary>
internal static class DispatcherErrors
{
    private const string ServiceNameRequiredCode = "dispatcher.config.service_required";
    private const string LifecycleNameRequiredCode = "dispatcher.config.lifecycle_name_required";
    private const string CodecServiceRequiredCode = "dispatcher.codec.service_required";
    private const string CodecProcedureRequiredCode = "dispatcher.codec.procedure_required";
    private const string CodecDuplicateCode = "dispatcher.codec.duplicate";
    private const string CodecRegistrationFailedCode = "dispatcher.codec.registration_failed";
    private const string ProcedureNameRequiredCode = "dispatcher.procedure.name_required";
    private const string ProcedureAliasInvalidCode = "dispatcher.procedure.alias_invalid";

    public static Error ServiceNameRequired() =>
        Error.From("Service name cannot be null or whitespace.", ServiceNameRequiredCode);

    public static Error LifecycleNameRequired() =>
        Error.From("Lifecycle name cannot be null or whitespace.", LifecycleNameRequiredCode);

    public static Error CodecServiceRequired() =>
        Error.From("Codec registration requires a service identifier.", CodecServiceRequiredCode);

    public static Error CodecProcedureRequired() =>
        Error.From("Codec registration requires a procedure name.", CodecProcedureRequiredCode);

    public static Error CodecDuplicate(string scope, string service, string procedure, ProcedureKind kind) =>
        Error.From("Codec already registered for the specified procedure.", CodecDuplicateCode)
            .WithMetadata("scope", scope)
            .WithMetadata("service", service)
            .WithMetadata("procedure", procedure)
            .WithMetadata("kind", kind.ToString());

    public static Error CodecRegistrationFailed(Error error) =>
        error.WithCode(CodecRegistrationFailedCode)
            .WithMetadata("stage", "dispatcher.codec.register");

    public static Error ProcedureNameRequired() =>
        Error.From("Procedure name cannot be null or whitespace.", ProcedureNameRequiredCode);

    public static Error ProcedureAliasInvalid() =>
        Error.From("Procedure aliases cannot contain null or whitespace entries.", ProcedureAliasInvalidCode);
}
