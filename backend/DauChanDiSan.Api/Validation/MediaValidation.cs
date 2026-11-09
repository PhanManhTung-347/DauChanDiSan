using DauChanDiSan.Api.Helpers;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Validation;

public static class MediaValidation
{
    public static void RequireOwner(int userId, params string?[] paths)
    {
        if (paths.Any(path => path is not null && !MediaPaths.BelongsTo(userId, path)))
            throw ApiFault.Invalid("Đường dẫn media phải là tệp do tài khoản hiện tại tải lên.");
    }

    public static void RequireOne(string? image, string? video)
    {
        if ((image is null) == (video is null))
            throw ApiFault.Invalid("Mỗi media cần đúng một đường dẫn ảnh hoặc video.");
        RequireKind(image, video);
    }

    public static void RequireKind(string? image, string? video)
    {
        if (image is not null && Path.GetExtension(image) is not (".jpg" or ".png" or ".webp"))
            throw ApiFault.Invalid("Trường ảnh phải là JPEG, PNG hoặc WebP.");
        if (video is not null && Path.GetExtension(video) != ".mp4")
            throw ApiFault.Invalid("Trường video phải là MP4.");
    }
}
