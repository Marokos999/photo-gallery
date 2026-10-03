using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace PhotoGallery.Upload;

public sealed class UploadFunction
{
    private static readonly TimeSpan UrlLifetime = TimeSpan.FromMinutes(5);

    private readonly IGalleryRepository _repository;
    private readonly IUrlSigner _signer;
    private readonly GalleryOptions _options;
    private readonly TimeProvider _time;

    public UploadFunction() : this(GalleryOptions.FromEnvironment())
    {
    }

    private UploadFunction(GalleryOptions options) : this(
        new DynamoGalleryRepository(AwsClientFactory.CreateDynamoDb(options), options),
        new S3UrlSigner(AwsClientFactory.CreateS3Presigner(options), options),
        options,
        TimeProvider.System)
    {
    }

    public UploadFunction(IGalleryRepository repository, IUrlSigner signer, GalleryOptions options, TimeProvider time)
    {
        _repository = repository;
        _signer = signer;
        _options = options;
        _time = time;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        var userId = UserResolver.Resolve(request, _options);
        if (userId is null)
            return HttpResults.Unauthorized();

        if (!UploadRequestValidator.TryValidate(ReadBody(request), out var upload, out var error))
            return HttpResults.BadRequest(error);

        var now = _time.GetUtcNow();
        var photoId = Keys.NewId(now);
        var photo = new Photo
        {
            UserId = userId,
            AlbumId = upload.AlbumId,
            PhotoId = photoId,
            OriginalKey = S3Keys.Original(userId, upload.AlbumId, photoId, FileNames.Sanitize(upload.FileName)),
            Caption = upload.Caption,
            Tags = upload.Tags,
            CreatedAt = now
        };

        try
        {
            await _repository.CreatePhotoAsync(photo);
        }
        catch (TransactionCanceledException)
        {
            return HttpResults.NotFound("Album not found.");
        }

        var expiresAt = now.Add(UrlLifetime);
        var form = await _signer.CreateUploadFormAsync(photo.OriginalKey, upload.ContentType, expiresAt);

        context.Logger.LogInformation("Upload URL created for photo {PhotoId} in album {AlbumId}", photoId, upload.AlbumId);

        return HttpResults.Ok(new UploadResponse(photoId, photo.OriginalKey, form.Url, form.Fields, expiresAt));
    }

    private static UploadRequest? ReadBody(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return null;

        var json = request.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
            : request.Body;

        try
        {
            return JsonSerializer.Deserialize<UploadRequest>(json, HttpResults.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}