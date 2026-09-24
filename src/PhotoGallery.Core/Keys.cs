namespace PhotoGallery.Core;

public static class Keys
{
    public const string AlbumSkPrefix = "ALBUM#";
    public const string PhotoSkPrefix = "PHOTO#";

    public static string UserPk(string userId) => $"USER#{userId}";

    public static string AlbumSk(string albumId) => $"{AlbumSkPrefix}{albumId}";

    public static string PhotoSk(string albumId, string photoId) => $"{PhotoSkPrefix}{albumId}#{photoId}";

    public static string PhotosInAlbumSkPrefix(string albumId) => $"{PhotoSkPrefix}{albumId}#";

    public static string AlbumGsiPk(string albumId) => $"ALBUM#{albumId}";

    public static string PhotoGsiSk(string photoId) => $"{PhotoSkPrefix}{photoId}";

    public static string NewId() => NewId(DateTimeOffset.UtcNow);

    public static string NewId(DateTimeOffset timestamp) => Guid.CreateVersion7(timestamp).ToString("N");
}