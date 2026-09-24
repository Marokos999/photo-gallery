using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using PhotoGallery.Core;

namespace PhotoGallery.Upload;

public static partial class UserResolver
{
    public const string DebugUserHeader = "x-debug-user-id";

    public static string? Resolve(APIGatewayHttpApiV2ProxyRequest request, GalleryOptions options)
    {
        if (request.RequestContext?.Authorizer?.Jwt?.Claims is { } claims
            && claims.TryGetValue("sub", out var sub))
            return Validated(sub);

        if (!options.UseLocalStack || request.Headers is null)
            return null;

        var debugUser = request.Headers
            .FirstOrDefault(h => string.Equals(h.Key, DebugUserHeader, StringComparison.OrdinalIgnoreCase))
            .Value;

        return Validated(debugUser);
    }

    private static string? Validated(string? userId) =>
        userId is not null && UserIdFormat().IsMatch(userId) ? userId : null;

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex UserIdFormat();
}