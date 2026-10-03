using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace PhotoGallery.Upload;

/// <summary>Request/response bodies (camelCase, like the Photos API).</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(UploadRequest))]
[JsonSerializable(typeof(UploadResponse))]
[JsonSerializable(typeof(ErrorResponse))]
public sealed partial class UploadJsonContext : JsonSerializerContext;

/// <summary>The API Gateway envelope, used by the Lambda runtime serializer.</summary>
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
public sealed partial class UploadLambdaJsonContext : JsonSerializerContext;
