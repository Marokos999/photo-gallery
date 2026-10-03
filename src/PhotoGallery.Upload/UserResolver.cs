using Amazon.Lambda.APIGatewayEvents;
using PhotoGallery.Core;
using PhotoGallery.Core.Auth;

namespace PhotoGallery.Upload;

public static class UserResolver
{
    public const string DebugUserHeader = UserIdentity.DebugUserHeader;

    public static string? Resolve(APIGatewayHttpApiV2ProxyRequest request, GalleryOptions options)
    {
        string? subject = null;
        request.RequestContext?.Authorizer?.Jwt?.Claims?.TryGetValue(UserIdentity.SubjectClaim, out subject);

        var debugUser = request.Headers?
            .FirstOrDefault(h => string.Equals(h.Key, DebugUserHeader, StringComparison.OrdinalIgnoreCase))
            .Value;

        return UserIdentity.Resolve(subject, debugUser, options);
    }
}
