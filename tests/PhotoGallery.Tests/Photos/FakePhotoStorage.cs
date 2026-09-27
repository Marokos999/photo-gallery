using System.Collections.Concurrent;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests.Photos;

internal sealed class FakePhotoStorage : IPhotoStorage
{
    public ConcurrentBag<string> DeletedKeys { get; } = new();

    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct = default)
    {
        foreach (var key in keys)
            DeletedKeys.Add(key);

        return Task.CompletedTask;
    }
}