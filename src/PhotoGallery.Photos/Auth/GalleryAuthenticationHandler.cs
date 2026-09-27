using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PhotoGallery.Core;

namespace PhotoGallery.Photos.Auth;

public sealed class GalleryAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder,
  GalleryOptions galleryOptions) :
 AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Gallery";
    public const string DebugUserHeader = "x-debug-user-id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // On AWS the Lambda adapter has already put the API Gateway JWT claims on Context.User.
        if (UserIdOf(Context.User) is not null)
            return Task.FromResult(Success(Context.User));

        if (galleryOptions.UseLocalStack
            && Request.Headers.TryGetValue(DebugUserHeader, out var header)
            && IdFormats.IsUserId(header.ToString()))
        {
            var identity = new ClaimsIdentity([new Claim("sub", header.ToString())], SchemeName);
            return Task.FromResult(Success(new ClaimsPrincipal(identity)));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    public static string? UserIdOf(ClaimsPrincipal user) =>
          user.FindFirst("sub")?.Value is { } sub && IdFormats.IsUserId(sub) ? sub : null;

    private static AuthenticateResult Success(ClaimsPrincipal principal) =>
        AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
}