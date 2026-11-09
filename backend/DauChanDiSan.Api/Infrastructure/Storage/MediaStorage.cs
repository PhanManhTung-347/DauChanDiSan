using System.Text.RegularExpressions;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Infrastructure.Storage;

public sealed partial class MediaStorage(IConfiguration config, IWebHostEnvironment environment, FirebaseStorage firebase)
{
    public const int Limit = 32 * 1024 * 1024;
    private bool Cloud => string.Equals(config["Storage:Provider"], "Firebase", StringComparison.OrdinalIgnoreCase);

    public async Task<string> UploadAsync(int ownerId, IFormFile file, CancellationToken ct)
    {
        if (Cloud) return await firebase.UploadAsync(ownerId, file, ct);
        if (file.Length is <= 0 or > Limit)
            throw ApiFault.Invalid("Tệp phải có kích thước từ 1 byte đến 32 MiB.");

        await using var input = file.OpenReadStream();
        var prefix = new byte[(int)Math.Min(12, file.Length)];
        await input.ReadExactlyAsync(prefix, ct);
        var (_, extension) = FirebaseStorage.Detect(prefix);
        input.Position = 0;
        var key = $"users/{ownerId}/{Guid.NewGuid():N}.{extension}";
        var path = LocalPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
            await input.CopyToAsync(output, ct);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return key;
    }

    public Task EnsureImageExistsAsync(string key, CancellationToken ct)
    {
        if (Cloud) return firebase.EnsureImageExistsAsync(key, ct);
        if (Path.GetExtension(key) == ".mp4" || !File.Exists(LocalPath(key)))
            throw ApiFault.Invalid("Ảnh đóng góp phải là ảnh đã tải lên.");
        return Task.CompletedTask;
    }

    public async Task<(Stream Content, string Type)> DownloadAsync(string key, CancellationToken ct)
    {
        if (Cloud)
        {
            var (content, type) = await firebase.DownloadAsync(key, ct);
            return (content, type);
        }
        var path = LocalPath(key);
        try
        {
            return (File.OpenRead(path), Path.GetExtension(key) switch
            {
                ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => "video/mp4"
            });
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            throw ApiFault.Missing("Không tìm thấy media.");
        }
    }

    private string LocalPath(string key)
    {
        if (!UploadKey().IsMatch(key)) throw ApiFault.Missing("Media không khả dụng.");
        var root = Path.GetFullPath(config["Storage:LocalRoot"] ?? "App_Data/uploads", environment.ContentRootPath);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        var relative = Path.GetRelativePath(webRoot, root);
        if (relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative))
            throw new ApiFault(503, "Thư mục lưu ảnh riêng tư phải nằm ngoài wwwroot.");
        return Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
    }

    [GeneratedRegex(@"\Ausers/[1-9][0-9]*/[a-f0-9]{32}\.(?:jpg|png|webp|mp4)\z", RegexOptions.CultureInvariant)]
    private static partial Regex UploadKey();
}
