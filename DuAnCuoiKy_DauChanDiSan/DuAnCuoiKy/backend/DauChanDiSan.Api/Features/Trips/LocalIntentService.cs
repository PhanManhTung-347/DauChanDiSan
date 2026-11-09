using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DauChanDiSan.Api.Shared;

namespace DauChanDiSan.Api.Features.Trips;

public sealed class LocalIntentService(IConfiguration configuration)
{
    public async Task<string> PredictAsync(string question, CancellationToken ct)
    {
        var pythonExe = configuration["LocalAI:PythonExe"];
        var scriptPath = configuration["LocalAI:ScriptPath"];

        if (string.IsNullOrWhiteSpace(pythonExe) || !File.Exists(pythonExe) ||
            string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            throw new ApiFault(503, "Kiểm tra LocalAI:PythonExe và LocalAI:ScriptPath.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-X");
        startInfo.ArgumentList.Add("utf8");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(question);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new ApiFault(503, "Không khởi động được Python.");
        }
        catch (Win32Exception)
        {
            throw new ApiFault(503, "Không chạy được Python. Hãy kiểm tra đường dẫn python.exe.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(timeout.Token));
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }

            ct.ThrowIfCancellationRequested();
            throw new ApiFault(504, "Mô hình phân loại mất quá lâu để phản hồi.");
        }

        if (process.ExitCode != 0)
            throw new ApiFault(502, "Python báo lỗi. Hãy chạy lại predict_intent.py trong terminal để kiểm tra.");

        try
        {
            using var json = JsonDocument.Parse(await stdout);

            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("yDinh", out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                var intent = value.GetString();

                if (intent is "TIM_DI_TICH" or "LAP_HANH_TRINH"
                    or "LAP_THEO_NGAN_SACH" or "SUA_HANH_TRINH")
                {
                    return intent;
                }
            }
        }
        catch (JsonException)
        {
            throw new ApiFault(502, "Python không trả về JSON hợp lệ.");
        }

        throw new ApiFault(502, "Mô hình trả về nhãn không hợp lệ.");
    }
}