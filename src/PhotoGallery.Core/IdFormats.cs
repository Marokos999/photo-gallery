using System.Text.RegularExpressions;

namespace PhotoGallery.Core;

public static partial class IdFormats
{
    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex EntityId();

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex UserId();
    public static bool IsEntityId(string? value) => value is not null && EntityId().IsMatch(value);
    public static bool IsUserId(string? value) => value is not null && UserId().IsMatch(value);
}