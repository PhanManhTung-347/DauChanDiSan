using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Features.Trips;

public sealed class LocalIntentService(
    IConfiguration config, IWebHostEnvironment environment, ILogger<LocalIntentService> logger)
{
    private string? ScriptPath
    {
        get
        {
            var configured = config["LocalAI:ScriptPath"];
            return string.IsNullOrWhiteSpace(configured) ? null
                : Path.GetFullPath(configured, environment.ContentRootPath);
        }
    }

    public bool IsConfigured => ScriptPath is { } path && File.Exists(path)
        && File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "intent_tree.joblib"));

    public async Task<string> PredictAsync(string question, CancellationToken ct)
    {
        var path = ScriptPath;
        if (path is null || !IsConfigured)
            throw new ApiFault(503, "Hãy cấu hình predict_intent.py và đặt intent_tree.joblib cùng thư mục.");

        var executable = config["LocalAI:PythonExe"];
        var start = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(executable) ? (OperatingSystem.IsWindows() ? "py" : "python3") : executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(path)!,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(path);
        start.ArgumentList.Add(question);
        start.Environment["PYTHONUTF8"] = "1";

        Process process;
        try { process = Process.Start(start) ?? throw new ApiFault(503, "Chưa khởi động được Python."); }
        catch (Win32Exception) { throw new ApiFault(503, "Kiểm tra cấu hình LocalAI:PythonExe."); }

        using (process)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errors = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await Task.WhenAll(process.WaitForExitAsync(timeout.Token), output, errors);
                if (process.ExitCode != 0)
                {
                    logger.LogWarning("Python phân loại ý định kết thúc với mã {Code}.", process.ExitCode);
                    throw new ApiFault(502, "Python không đọc được mô hình. Hãy thử chạy predict_intent.py trong terminal.");
                }

                using var json = JsonDocument.Parse(await output);
                if (json.RootElement.ValueKind == JsonValueKind.Object
                    && json.RootElement.TryGetProperty("yDinh", out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    var intent = value.GetString();
                    if (intent is "TIM_DI_TICH" or "LAP_HANH_TRINH" or "LAP_THEO_NGAN_SACH" or "SUA_HANH_TRINH")
                        return intent;
                }
                throw new ApiFault(502, "Kết quả phân loại từ Python không hợp lệ.");
            }
            catch (JsonException) { throw new ApiFault(502, "Python cần trả một đối tượng JSON có trường yDinh."); }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
                ct.ThrowIfCancellationRequested();
                throw new ApiFault(504, "Mô hình phân loại mất quá lâu để phản hồi.");
            }
        }
    }
}