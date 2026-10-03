using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests;

public class CachingUrlSignerTests
{
    private readonly CountingSigner _inner = new();
    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly CachingUrlSigner _signer;

    public CachingUrlSignerTests() => _signer = new CachingUrlSigner(_inner, _time);

    [Fact]
    public async Task SameKey_ReturnsIdenticalUrl_WithinReuseWindow()
    {
        var first = await _signer.CreateDownloadUrlAsync("thumbs/a_400.webp");
        _time.Advance(CachingUrlSigner.ReuseWindow - TimeSpan.FromSeconds(1));
        var second = await _signer.CreateDownloadUrlAsync("thumbs/a_400.webp");

        Assert.Equal(first, second);
        Assert.Equal(1, _inner.DownloadCalls);
    }

    [Fact]
    public async Task SameKey_IsResigned_AfterReuseWindow()
    {
        var first = await _signer.CreateDownloadUrlAsync("thumbs/a_400.webp");
        _time.Advance(CachingUrlSigner.ReuseWindow);
        var second = await _signer.CreateDownloadUrlAsync("thumbs/a_400.webp");

        Assert.NotEqual(first, second);
        Assert.Equal(2, _inner.DownloadCalls);
    }

    [Fact]
    public async Task DifferentKeys_GetDifferentUrls()
    {
        var a = await _signer.CreateDownloadUrlAsync("thumbs/a_400.webp");
        var b = await _signer.CreateDownloadUrlAsync("thumbs/b_400.webp");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task UploadForms_AreNeverCached()
    {
        await _signer.CreateUploadFormAsync("originals/k", "image/jpeg", _time.GetUtcNow().AddMinutes(5));
        await _signer.CreateUploadFormAsync("originals/k", "image/jpeg", _time.GetUtcNow().AddMinutes(5));

        Assert.Equal(2, _inner.UploadCalls);
    }

    private sealed class CountingSigner : IUrlSigner
    {
        public int DownloadCalls { get; private set; }
        public int UploadCalls { get; private set; }

        public Task<string> CreateDownloadUrlAsync(string key) =>
            Task.FromResult($"https://signed.test/{key}?n={++DownloadCalls}");

        public Task<UploadForm> CreateUploadFormAsync(string key, string contentType, DateTimeOffset expiresAt)
        {
            UploadCalls++;
            return Task.FromResult(new UploadForm("https://signed.test", new Dictionary<string, string>()));
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
