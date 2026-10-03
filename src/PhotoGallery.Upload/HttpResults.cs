using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Amazon.Lambda.APIGatewayEvents;

namespace PhotoGallery.Upload;

public static class HttpResults
{
    private static readonly UploadJsonContext Json = UploadJsonContext.Default;

    public static APIGatewayHttpApiV2ProxyResponse Ok(UploadResponse body) => Create(200, body, Json.UploadResponse);

    public static APIGatewayHttpApiV2ProxyResponse BadRequest(string error) => Error(400, error);

    public static APIGatewayHttpApiV2ProxyResponse Unauthorized() => Error(401, "Unauthorized.");

    public static APIGatewayHttpApiV2ProxyResponse NotFound(string error) => Error(404, error);

    private static APIGatewayHttpApiV2ProxyResponse Error(int statusCode, string error) =>
        Create(statusCode, new ErrorResponse(error), Json.ErrorResponse);

    private static APIGatewayHttpApiV2ProxyResponse Create<T>(int statusCode, T body, JsonTypeInfo<T> typeInfo) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(body, typeInfo),
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
    };
}
