namespace OmniRelay.Dispatcher;

/// <summary>
/// Stores metadata about a codec registration, including the concrete codec instance and the typed message contracts.
/// </summary>
public sealed record ProcedureCodecDescriptor(
    Type RequestType,
    Type ResponseType,
    object Codec,
    string Encoding)
{
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
}

