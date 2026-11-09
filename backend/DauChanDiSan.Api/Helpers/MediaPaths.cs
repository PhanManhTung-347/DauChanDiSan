namespace DauChanDiSan.Api.Helpers;

public static class MediaPaths
{
    public static bool BelongsTo(int userId, string path)
    {
        var prefix = $"users/{userId}/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var file = path[prefix.Length..];
        var extension = Path.GetExtension(file);
        return extension is ".jpg" or ".png" or ".webp" or ".mp4"
            && file.Length == 32 + extension.Length && Guid.TryParseExact(file[..32], "N", out _);
    }

}
