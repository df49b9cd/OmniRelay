using OmniRelay.Dispatcher;

namespace ProtobufIncrementalSample;

public static class GeneratedUsage
{
    public static void Use(Dispatcher dispatcher)
    {
        var client = TestServiceOmniRelay.CreateTestServiceClient(dispatcher, "yarpcore.tests.codegen");
        _ = client;
    }
}
