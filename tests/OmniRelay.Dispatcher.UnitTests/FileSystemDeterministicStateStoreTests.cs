using AwesomeAssertions;
using Hugo;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public sealed class FileSystemDeterministicStateStoreTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void Set_OverwritesExistingRecords()
    {
        using var temp = new TempDirectory();
        var storeResult = FileSystemDeterministicStateStore.Create(temp.Path);
        storeResult.IsSuccess.Should().BeTrue(storeResult.Error?.ToString());
        var store = storeResult.Value;

        var record1 = new DeterministicRecord("kind", 1, [1], DateTimeOffset.UtcNow);
        store.Set("key", record1);

        var record2 = new DeterministicRecord("kind", 2, [2], DateTimeOffset.UtcNow.AddMinutes(1));
        store.Set("key", record2);

        store.TryGet("key", out var fetched).Should().BeTrue();
        fetched.Version.Should().Be(2);
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void TryAdd_ReturnsFalseWhenFileExists()
    {
        using var temp = new TempDirectory();
        var storeResult = FileSystemDeterministicStateStore.Create(temp.Path);
        storeResult.IsSuccess.Should().BeTrue(storeResult.Error?.ToString());
        var store = storeResult.Value;
        var record = new DeterministicRecord("kind", 1, [1], DateTimeOffset.UtcNow);

        store.TryAdd("key", record).Should().BeTrue();
        store.TryAdd("key", record).Should().BeFalse();
    }

    [Fact(Timeout = TestTimeouts.Default)]
    public void Set_And_Get_Handle_Long_Keys()
    {
        using var temp = new TempDirectory();
        var storeResult = FileSystemDeterministicStateStore.Create(temp.Path);
        storeResult.IsSuccess.Should().BeTrue(storeResult.Error?.ToString());
        var store = storeResult.Value;

        var longKey = new string('k', 2_048);
        var payload = Enumerable.Range(0, 256).Select(static i => (byte)i).ToArray();
        var record = new DeterministicRecord("kind", 3, payload, DateTimeOffset.UtcNow);

        store.Set(longKey, record);

        store.TryGet(longKey, out var fetched).Should().BeTrue();
        fetched.Payload.ToArray().Should().Equal(payload);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fs-store-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
