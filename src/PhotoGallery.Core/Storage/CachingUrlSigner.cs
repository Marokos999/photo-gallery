using System.Collections.Concurrent;

namespace PhotoGallery.Core.Storage;

/// <summary>
/// Reuses download URLs per object key. Presigned URLs embed the signing time, so signing on every request
/// produces a new URL each time and the browser re-downloads every thumbnail whenever the album is polled.
/// Reusing the URL keeps it byte-for-byte identical, so browser caching (and React's structural sharing) work.
/// Upload forms are never cached.
/// </summary>
public sealed class CachingUrlSigner(IUrlSigner inner, TimeProvider time) : IUrlSigner
{
    /// <summary>A reused URL is always valid for at least <c>DownloadUrlLifetime - ReuseWindow</c> (15 minutes).</summary>
    public static readonly TimeSpan ReuseWindow = TimeSpan.FromMinutes(45);

    private const int PruneThreshold = 10_000;

    private readonly ConcurrentDictionary<string, CachedUrl> _cache = new();

    public Task<UploadForm> CreateUploadFormAsync(string key, string contentType, DateTimeOffset expiresAt) =>
        inner.CreateUploadFormAsync(key, contentType, expiresAt);

    public async Task<string> CreateDownloadUrlAsync(string key)
    {
        var now = time.GetUtcNow();
        if (_cache.TryGetValue(key, out var cached) && cached.ReuseUntil > now)
            return cached.Url;

        var url = await inner.CreateDownloadUrlAsync(key);
        _cache[key] = new CachedUrl(url, now.Add(ReuseWindow));

        if (_cache.Count > PruneThreshold)
            Prune(now);

        return url;
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var (key, entry) in _cache)
        {
            if (entry.ReuseUntil <= now)
                _cache.TryRemove(key, out _);
        }
    }

    private sealed record CachedUrl(string Url, DateTimeOffset ReuseUntil);
}
