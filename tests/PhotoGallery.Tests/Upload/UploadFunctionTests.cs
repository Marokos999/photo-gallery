using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Upload;

namespace PhotoGallery.Tests.Upload;

public class UploadFunctionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly GalleryOptions LocalOptions = new("table", "bucket", "http://localstack:4566");
    private static readonly GalleryOptions AwsOptions = new("table", "bucket", null);

    private readonly FakeGalleryRepository _repository = new();
    private readonly string _albumId = Keys.NewId();

    [Fact]
    public async Task Returns401_WithoutUser()
    {
        var response = await Handle(Request(ValidBody(), userId: null));

        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task Returns401_ForDebugHeader_OutsideLocalStack()
    {
        var response = await Handle(Request(ValidBody()), AwsOptions);

        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task Returns400_ForUnsupportedContentType()
    {
        var response = await Handle(Request(ValidBody() with { ContentType = "application/pdf" }));

        Assert.Equal(400, response.StatusCode);
        Assert.Empty(_repository.CreatedPhotos);
    }

    [Fact]
    public async Task Returns400_ForPathTraversalAlbumId()
    {
        var response = await Handle(Request(ValidBody() with { AlbumId = "../../other-user" }));

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task Returns404_WhenAlbumDoesNotExist()
    {
        _repository.AlbumExists = false;

        var response = await Handle(Request(ValidBody()));

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task Returns200_AndCreatesPendingPhoto()
    {
        var response = await Handle(Request(ValidBody()));

        Assert.Equal(200, response.StatusCode);
        var body = JsonSerializer.Deserialize<UploadResponse>(response.Body, HttpResults.JsonOptions)!;
        var photo = Assert.Single(_repository.CreatedPhotos);

        Assert.Equal(PhotoStatus.Pending, photo.Status);
        Assert.Equal($"originals/user-1/{_albumId}/{body.PhotoId}/My-Photo.jpg", photo.OriginalKey);
        Assert.Equal(photo.OriginalKey, body.Key);
        Assert.Contains("contentType=image/jpeg", body.UploadUrl);
        Assert.Equal(Now.AddMinutes(5), body.ExpiresAt);
        Assert.Equal(["beach"], photo.Tags);
    }

    [Fact]
    public async Task PrefersJwtSubClaim_OverDebugHeader()
    {
        var request = Request(ValidBody());
        request.RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Jwt = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription.JwtDescription
                {
                    Claims = new Dictionary<string, string> { ["sub"] = "cognito-user" }
                }
            }
        };

        await Handle(request);

        Assert.Equal("cognito-user", Assert.Single(_repository.CreatedPhotos).UserId);
    }

    private UploadRequest ValidBody() => new("My Photo.jpg", "image/jpeg", _albumId, "Sunset", ["beach"]);

    private static APIGatewayHttpApiV2ProxyRequest Request(UploadRequest body, string? userId = "user-1") => new()
    {
        Body = JsonSerializer.Serialize(body, HttpResults.JsonOptions),
        Headers = userId is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { [UserResolver.DebugUserHeader] = userId }
    };

    private Task<APIGatewayHttpApiV2ProxyResponse> Handle(
        APIGatewayHttpApiV2ProxyRequest request,
        GalleryOptions? options = null)
    {
        var function = new UploadFunction(_repository, new FakeUrlSigner(), options ?? LocalOptions, new FixedTimeProvider(Now));
        return function.HandleAsync(request, new TestLambdaContext());
    }
}