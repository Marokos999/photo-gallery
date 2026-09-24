using System.Net.Http.Headers;
using PhotoGallery.Core;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests.Integration;

[Trait("Category", "Integration")]
public class S3UrlSignerTests
{
    private const string SkipReason = "LocalStack is not running on 127.0.0.1:4566";

    private readonly S3UrlSigner _signer =
        new(AwsClientFactory.CreateS3Presigner(LocalStack.Options), LocalStack.Options);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UploadUrl_AcceptsPut_AndDownloadUrl_ReturnsSameBytes()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var key = S3Keys.Original(Keys.NewId(), Keys.NewId(), Keys.NewId(), "test.jpg");
        byte[] payload = [0xFF, 0xD8, 0xFF, 0xE0];
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        using var http = new HttpClient();

        var uploadUrl = await _signer.CreateUploadUrlAsync(key, "image/jpeg", expiresAt);
        using var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var put = await http.PutAsync(uploadUrl, content, Ct);

        Assert.True(put.IsSuccessStatusCode, $"PUT failed: {(int)put.StatusCode}");
        Assert.StartsWith("http://127.0.0.1:4566/", uploadUrl);

        var downloadUrl = await _signer.CreateDownloadUrlAsync(key, expiresAt);
        Assert.Equal(payload, await http.GetByteArrayAsync(downloadUrl, Ct));
    }
}