using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

namespace PhotoGallery.Upload;

public static class HttpResults
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static APIGatewayHttpApiV2ProxyResponse Ok<T>(T body) => Create(200, body);

    public static APIGatewayHttpApiV2ProxyResponse BadRequest(string error) => Create(400, new ErrorResponse(error));

    public static APIGatewayHttpApiV2ProxyResponse Unauthorized() => Create(401, new ErrorResponse("Unauthorized."));

    public static APIGatewayHttpApiV2ProxyResponse NotFound(string error) => Create(404, new ErrorResponse(error));

    private static APIGatewayHttpApiV2ProxyResponse Create<T>(int statusCode, T body) => new()
    {
        StatusCode = statusCode,
        Body = JsonSerializer.Serialize(body, JsonOptions),
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
    };
}