using System.Security.Claims;

namespace PhotoGallery.Photos.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) => GalleryAuthenticationHandler.UserIdOf(user)
        ?? throw new InvalidOperationException("Endpoint requires an authenticated user.");
}