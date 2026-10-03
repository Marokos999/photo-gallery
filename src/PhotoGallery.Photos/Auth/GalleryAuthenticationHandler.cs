using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PhotoGallery.Core;
using PhotoGallery.Core.Auth;

namespace PhotoGallery.Photos.Auth;

public sealed class GalleryAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    GalleryOptions galleryOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Gallery";
    public const string DebugUserHeader = UserIdentity.DebugUserHeader;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // On AWS the Lambda adapter has already put the API Gateway JWT claims on Context.User.
        var subject = Context.User.FindFirst(UserIdentity.SubjectClaim)?.Value;
        var userId = UserIdentity.Resolve(subject, Request.Headers[DebugUserHeader].FirstOrDefault(), galleryOptions);

        if (userId is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = subject is not null
            ? Context.User
            : new ClaimsPrincipal(new ClaimsIdentity([new Claim(UserIdentity.SubjectClaim, userId)], SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    public static string? UserIdOf(ClaimsPrincipal user) =>
        user.FindFirst(UserIdentity.SubjectClaim)?.Value is { } sub && IdFormats.IsUserId(sub) ? sub : null;
}
