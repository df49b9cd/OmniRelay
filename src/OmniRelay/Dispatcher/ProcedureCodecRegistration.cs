using System.Collections.Immutable;

namespace OmniRelay.Dispatcher;

internal sealed record ProcedureCodecRegistration(
    ProcedureCodecScope Scope,
    string? Service,
    string Procedure,
    ProcedureKind Kind,
    Type RequestType,
    Type ResponseType,
    object Codec,
    string Encoding,
    ImmutableArray<string> Aliases)
{
    public ProcedureCodecScope Scope
    {
        get => field;
        init => field = value;
    } = Scope;

    public string? Service
    {
        get => field;
        init => field = value;
    } = Service;

    public string Procedure
    {
        get => field;
        init => field = value;
    } = Procedure;

    public ProcedureKind Kind
    {
        get => field;
        init => field = value;
    } = Kind;

    public Type RequestType
    {
        get => field;
        init => field = value;
    } = RequestType;

    public Type ResponseType
    {
        get => field;
        init => field = value;
    } = ResponseType;

    public object Codec
    {
        get => field;
        init => field = value;
    } = Codec;

    public string Encoding
    {
        get => field;
        init => field = value;
    } = Encoding;

    public ImmutableArray<string> Aliases
    {
        get => field;
        init => field = value;
    } = Aliases;
}

