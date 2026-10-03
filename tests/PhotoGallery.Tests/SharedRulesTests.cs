using PhotoGallery.Core;
using PhotoGallery.Core.Auth;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Tests;

public class SharedRulesTests
{
    private static readonly GalleryOptions Local = new("t", "b", "http://localstack:4566");
    private static readonly GalleryOptions Aws = new("t", "b", null);

    [Fact]
    public void Normalize_TrimsDropsEmptyAndDeduplicatesTags()
    {
        var details = PhotoDetailsRules.Normalize("  Sunset ", [" sea", "Sea ", "", null, "beach"]);

        Assert.Equal("Sunset", details.Caption);
        Assert.Equal(["sea", "beach"], details.Tags);
    }

    [Fact]
    public void Normalize_BlankCaptionBecomesNull() =>
        Assert.Null(PhotoDetailsRules.Normalize("   ", null).Caption);

    [Fact]
    public void Validate_ReportsCaptionAndTagErrors()
    {
        var details = new PhotoDetails(new string('x', 501), Enumerable.Range(0, 21).Select(i => $"t{i}").ToList());

        var errors = PhotoDetailsRules.Validate(details);

        Assert.Contains("caption", errors.Keys);
        Assert.Contains("tags", errors.Keys);
    }

    [Theory]
    [InlineData("cognito-sub", "marko", true, "cognito-sub")] // JWT wins over the header
    [InlineData(null, "marko", true, "marko")]                 // debug header against LocalStack
    [InlineData(null, "marko", false, null)]                   // debug header ignored on AWS
    [InlineData("bad id!", "marko", true, null)]               // invalid JWT subject is not replaced by the header
    [InlineData(null, "../etc", true, null)]                   // invalid header value
    public void UserIdentity_Resolve(string? jwtSubject, string? header, bool localStack, string? expected) =>
        Assert.Equal(expected, UserIdentity.Resolve(jwtSubject, header, localStack ? Local : Aws));
}
