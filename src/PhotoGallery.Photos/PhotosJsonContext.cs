using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Photos;

/// <summary>
/// Compile-time JSON metadata for every request and response body: no reflection on the hot path,
/// which trims Lambda cold starts. Registered first in the resolver chain in Program.cs.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(CreateAlbumRequest))]
[JsonSerializable(typeof(RenameAlbumRequest))]
[JsonSerializable(typeof(AlbumResponse))]
[JsonSerializable(typeof(List<AlbumResponse>))]
[JsonSerializable(typeof(UpdatePhotoRequest))]
[JsonSerializable(typeof(PhotoResponse))]
[JsonSerializable(typeof(PhotoPageResponse))]
[JsonSerializable(typeof(IReadOnlyList<TagCount>))]
[JsonSerializable(typeof(CreateShareRequest))]
[JsonSerializable(typeof(ShareResponse))]
[JsonSerializable(typeof(List<ShareSummaryResponse>))]
[JsonSerializable(typeof(SharedAlbumResponse))]
public sealed partial class PhotosJsonContext : JsonSerializerContext;

/// <summary>The API Gateway envelope the Lambda runtime deserializes before ASP.NET Core sees the request.</summary>
[JsonSerializable(typeof(Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse))]
public sealed partial class LambdaEventsJsonContext : JsonSerializerContext;
