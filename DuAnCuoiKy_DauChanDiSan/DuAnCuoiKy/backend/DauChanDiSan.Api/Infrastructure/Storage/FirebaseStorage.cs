using System.Net;
using DauChanDiSan.Api.Shared;
using Google;
using Google.Cloud.Storage.V1;

namespace DauChanDiSan.Api.Infrastructure.Storage;

public sealed class FirebaseStorage(IConfiguration configuration)
{
    private const int Limit = MediaStorage.Limit;
    private readonly Lazy<Task<StorageClient>> client = new(() => StorageClient.CreateAsync());

    private string Bucket => configuration["Firebase:StorageBucket"] is { Length: > 0 } bucket
        ? bucket : throw new ApiFault(503, "Chưa cấu hình Firebase:StorageBucket.");

    public async Task<string> UploadAsync(int ownerId, IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > Limit)
            throw ApiFault.Invalid("Tệp phải có kích thước từ 1 byte đến 32 MiB.");
        await using var stream = file.OpenReadStream();
        var prefix = new byte[(int)Math.Min(12, file.Length)];
        await stream.ReadExactlyAsync(prefix, ct);
        var (type, extension) = Detect(prefix);
        stream.Seek(0, SeekOrigin.Begin);
        var path = $"users/{ownerId}/{Guid.NewGuid():N}.{extension}";
        var bucket = Bucket;
        try
        {
            await (await GetClientAsync()).UploadObjectAsync(bucket, path, type, stream, cancellationToken: ct);
        }
        catch (GoogleApiException)
        {
            throw new ApiFault(502, "Firebase Storage từ chối tải tệp lên.");
        }
        return path;
    }

    public async Task EnsureImageExistsAsync(string path, CancellationToken ct)
    {
        var bucket = Bucket;
        try
        {
            var metadata = await (await GetClientAsync()).GetObjectAsync(bucket, path, cancellationToken: ct);
            if (metadata.Size is null or 0 or > Limit ||
                metadata.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
                throw ApiFault.Invalid("Tệp đóng góp phải là ảnh đã tải lên.");
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw ApiFault.Invalid("Ảnh đóng góp không còn tồn tại trong Storage.");
        }
        catch (GoogleApiException)
        {
            throw new ApiFault(502, "Firebase Storage từ chối đọc ảnh.");
        }
    }

    private async Task<StorageClient> GetClientAsync()
    {
        try { return await client.Value; }
        catch (InvalidOperationException) { throw new ApiFault(503, "Chưa cấu hình thông tin xác thực Firebase Storage."); }
    }

    public async Task<(MemoryStream Content, string Type)> DownloadAsync(string path, CancellationToken ct)
    {
        var bucket = Bucket;
        try
        {
            var storage = await GetClientAsync();
            var metadata = await storage.GetObjectAsync(bucket, path, cancellationToken: ct);
            if (metadata.Size is null or > Limit ||
                metadata.ContentType is not ("image/jpeg" or "image/png" or "image/webp" or "video/mp4"))
                throw ApiFault.Invalid("Định dạng hoặc kích thước media không được hỗ trợ.");
            var content = new MemoryStream((int)metadata.Size.Value);
            try
            {
                await storage.DownloadObjectAsync(bucket, path, content,
                    new DownloadObjectOptions { Generation = metadata.Generation }, ct);
                content.Position = 0;
                return (content, metadata.ContentType);
            }
            catch
            {
                await content.DisposeAsync();
                throw;
            }
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw ApiFault.Missing("Tệp không tồn tại trong Firebase Storage.");
        }
        catch (GoogleApiException)
        {
            throw new ApiFault(502, "Firebase Storage từ chối đọc tệp.");
        }
    }

    internal static (string, string) Detect(ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length >= 3 && prefix[0] == 255 && prefix[1] == 216 && prefix[2] == 255)
            return ("image/jpeg", "jpg");
        if (prefix.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ("image/png", "png");
        if (prefix.Length >= 12 && prefix[..4].SequenceEqual("RIFF"u8) && prefix[8..12].SequenceEqual("WEBP"u8))
            return ("image/webp", "webp");
        if (prefix.Length >= 12 && prefix[4..8].SequenceEqual("ftyp"u8))
            return ("video/mp4", "mp4");
        throw ApiFault.Invalid("Chỉ nhận JPEG, PNG, WebP và MP4.");
    }
}
