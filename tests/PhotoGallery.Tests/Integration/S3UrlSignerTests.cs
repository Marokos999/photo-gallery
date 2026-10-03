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
    public async Task UploadForm_AcceptsPost_AndDownloadUrl_ReturnsSameBytes()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var key = NewKey();
        byte[] payload = [0xFF, 0xD8, 0xFF, 0xE0];
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        using var http = new HttpClient();

        var form = await _signer.CreateUploadFormAsync(key, "image/jpeg", expiresAt);
        using var response = await PostAsync(http, form, payload, "image/jpeg");

        Assert.True(response.IsSuccessStatusCode, $"POST failed: {(int)response.StatusCode}");
        Assert.StartsWith("http://127.0.0.1:4566/", form.Url);

        var downloadUrl = await _signer.CreateDownloadUrlAsync(key);
        Assert.Equal(payload, await http.GetByteArrayAsync(downloadUrl, Ct));
    }

    [Fact]
    public async Task UploadForm_RejectsFileOverSizeLimit()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        using var http = new HttpClient();
        var form = await _signer.CreateUploadFormAsync(NewKey(), "image/jpeg", DateTimeOffset.UtcNow.AddMinutes(5));

        using var response = await PostAsync(http, form, new byte[UploadLimits.MaxFileBytes + 1], "image/jpeg");

        Assert.False(response.IsSuccessStatusCode, "S3 accepted a file above the policy size limit.");
    }

    [Fact]
    public async Task UploadForm_RejectsDifferentContentType()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        using var http = new HttpClient();
        var form = await _signer.CreateUploadFormAsync(NewKey(), "image/jpeg", DateTimeOffset.UtcNow.AddMinutes(5));
        var tampered = new UploadForm(form.Url, new Dictionary<string, string>(form.Fields) { ["Content-Type"] = "text/html" });

        using var response = await PostAsync(http, tampered, [1, 2, 3], "text/html");

        Assert.False(response.IsSuccessStatusCode, "S3 accepted a content type the policy does not allow.");
    }

    private static string NewKey() => S3Keys.Original(Keys.NewId(), Keys.NewId(), Keys.NewId(), "test.jpg");

    // Same request a browser makes: policy fields first, the file last.
    private static Task<HttpResponseMessage> PostAsync(HttpClient http, UploadForm form, byte[] file, string contentType)
    {
        var body = new MultipartFormDataContent();
        foreach (var (name, value) in form.Fields)
            body.Add(new StringContent(value), name);

        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        body.Add(fileContent, "file", "test.jpg");

        return http.PostAsync(form.Url, body, Ct);
    }
}
