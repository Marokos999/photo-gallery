using System.Text.RegularExpressions;

namespace PhotoGallery.Core.Storage;

public static partial class FileNames
{
    private const int MaxLength = 100;
    private const string Fallback = "photo";

    public static string Sanitize(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        name = UnsafeCharacters().Replace(name, "-").Trim('-', '.');

        if (name.Length > MaxLength)
            name = name[^MaxLength..];

        return name.Length == 0 ? Fallback : name;
    }

    [GeneratedRegex("[^A-Za-z0-9._-]+")]
    private static partial Regex UnsafeCharacters();
}