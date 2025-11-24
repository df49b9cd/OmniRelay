using AwesomeAssertions;
using Hugo;
using Microsoft.Data.Sqlite;
using Xunit;

namespace OmniRelay.Dispatcher.UnitTests;

public sealed class SqliteDeterministicStateStoreTests
{
    [Fact(Timeout = TestTimeouts.Default)]
    public void TryAdd_OnlySucceedsForFirstWriter()
    {
        using var temp = new TempFile();
        var storeResult = SqliteDeterministicStateStore.Create($"Data Source={temp.Path}");
        storeResult.IsSuccess.Should().BeTrue(storeResult.Error?.ToString());
        var store = storeResult.Value;
        var record = new DeterministicRecord("kind", 1, [1, 2], DateTimeOffset.UtcNow);

        store.TryAdd("key", record).Should().BeTrue();
        store.TryAdd("key", record).Should().BeFalse();

        store.TryGet("key", out var fetched).Should().BeTrue();
        fetched.Kind.Should().Be(record.Kind);
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (!File.Exists(Path))
            {
                return;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    File.Delete(Path);
                    break;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
