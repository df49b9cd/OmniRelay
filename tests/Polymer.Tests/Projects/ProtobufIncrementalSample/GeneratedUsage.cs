using Polymer.Tests.Protos;

namespace ProtobufIncrementalSample;

public static class GeneratedUsage
{
    public static void Use(Polymer.Dispatcher.Dispatcher dispatcher)
    {
        var client = TestServicePolymer.CreateTestServiceClient(dispatcher, "polymer.tests.codegen");
        _ = client;
    }
}
