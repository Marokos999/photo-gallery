using System.Buffers.Text;
using System.Security.Cryptography;

namespace PhotoGallery.Core;

public static class Keys
{
    public const string AlbumSkPrefix = "ALBUM#";
    public const string PhotoSkPrefix = "PHOTO#";
    public const string ShareSk = "SHARE";

    public static string UserPk(string userId) => $"USER#{userId}";

    public static string AlbumSk(string albumId) => $"{AlbumSkPrefix}{albumId}";

    public static string PhotoSk(string albumId, string photoId) => $"{PhotoSkPrefix}{albumId}#{photoId}";

    public static string PhotosInAlbumSkPrefix(string albumId) => $"{PhotoSkPrefix}{albumId}#";

    public static string AlbumGsiPk(string albumId) => $"ALBUM#{albumId}";

    public const string ShareGsiSkPrefix = "SHARE#";

    public static string ShareGsiSk(string code) => $"{ShareGsiSkPrefix}{code}";

    public static string NewId() => NewId(DateTimeOffset.UtcNow);

    public static string NewId(DateTimeOffset timestamp) => Guid.CreateVersion7(timestamp).ToString("N");

    public static string SharePk(string code) => $"SHARE#{code}";

    public static string NewShareCode() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
}