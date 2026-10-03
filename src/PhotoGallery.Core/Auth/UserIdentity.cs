namespace PhotoGallery.Core.Auth;

/// <summary>
/// Who is calling — one rule for every entry point (upload Lambda and Photos API):
/// the API Gateway JWT <c>sub</c> claim wins; the debug header is honoured only against LocalStack.
/// </summary>
public static class UserIdentity
{
    public const string SubjectClaim = "sub";
    public const string DebugUserHeader = "x-debug-user-id";

    public static string? Resolve(string? jwtSubject, string? debugUserHeader, GalleryOptions options)
    {
        if (jwtSubject is not null)
            return IdFormats.IsUserId(jwtSubject) ? jwtSubject : null;

        return options.UseLocalStack && IdFormats.IsUserId(debugUserHeader) ? debugUserHeader : null;
    }
}
