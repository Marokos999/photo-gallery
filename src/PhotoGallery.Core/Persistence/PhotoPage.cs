using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public sealed record PhotoPage(IReadOnlyList<Photo> Items, string? NextCursor);

/// <summary>
/// Opaque pagination cursor: the Base64Url-encoded sort key of the last item returned.
/// Decoding checks the key belongs to the requested album, so a cursor cannot be pointed at other data.
/// </summary>
public static class PageCursor
{
    public static string Encode(string sortKey) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(sortKey));

    public static bool TryDecode(string? cursor, string requiredPrefix, [NotNullWhen(true)] out string? sortKey)
    {
        sortKey = null;
        if (string.IsNullOrEmpty(cursor))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
            if (!decoded.StartsWith(requiredPrefix, StringComparison.Ordinal))
                return false;

            sortKey = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
