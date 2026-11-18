using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using JobPortal.Configuration;
using Microsoft.Extensions.Options;

namespace JobPortal.Services;

public interface IInterviewSpeechService
{
    Task<string> ChuyenThanhVanBanAsync(Stream wav, CancellationToken token);
}

public sealed class InterviewSpeechService(IWebHostEnvironment environment, IOptions<AiInterviewOptions> options,
    AiInterviewRuntime runtime, ILogger<InterviewSpeechService> logger) : IInterviewSpeechService
{
    public const int MaxAudioBytes = 16000 * 2 * 120 + 44;

    public Task<string> ChuyenThanhVanBanAsync(Stream wav, CancellationToken token) => runtime.ChayDocQuyenAsync(async () =>
    {
        using var input = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await wav.ReadAsync(buffer, token)) > 0)
        {
            if (input.Length + read > MaxAudioBytes) throw new AiInterviewException("Mỗi câu trả lời ghi âm tối đa 2 phút.", 400);
            input.Write(buffer, 0, read);
        }
        var data = input.ToArray();
        KiemTraAmThanh(data);
        var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.SpeechModelPath));
        var python = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.SpeechPythonPath));
        var script = Path.Combine(environment.ContentRootPath, "Backend", "Scripts", "NhanGiongNoi.py");
        if (!Directory.Exists(path) || !File.Exists(python) || !File.Exists(script))
            throw new AiInterviewException("Nhận giọng nói hiện chưa sẵn sàng. Bạn có thể nhập câu trả lời hoặc thử lại sau.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.Value.SpeechTimeoutSeconds, 60, 900)));
            // A private, short-lived CPU worker uses the model's supported Transformers decoder.
            // WAV goes through stdin, never a temporary audio file. Worker exit releases model memory.
            var start = new ProcessStartInfo(python)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = environment.ContentRootPath
            };
            start.ArgumentList.Add(script); start.ArgumentList.Add(path);
            var storage = Path.Combine(environment.ContentRootPath, "Backend", "Storage", "AI");
            Directory.CreateDirectory(Path.Combine(storage, "tmp"));
            start.Environment["HF_HOME"] = Path.Combine(storage, "tools", "hf-cache");
            start.Environment["TORCH_HOME"] = Path.Combine(storage, "tools", "torch-cache");
            start.Environment["HF_HUB_OFFLINE"] = "1";
            start.Environment["TRANSFORMERS_OFFLINE"] = "1";
            start.Environment["HF_HUB_DISABLE_TELEMETRY"] = "1";
            start.Environment["PYTHONIOENCODING"] = "utf-8";
            start.Environment["PYTHONNOUSERSITE"] = "1";
            start.Environment["TEMP"] = Path.Combine(storage, "tmp");
            start.Environment["TMP"] = Path.Combine(storage, "tmp");
            using var process = new Process { StartInfo = start };
            process.Start();
            var output = DocKetQuaAsync(process.StandardOutput, timeout.Token);
            var errors = DocBoThongBaoAsync(process.StandardError, timeout.Token);
            try
            {
                await process.StandardInput.BaseStream.WriteAsync(data, timeout.Token);
                process.StandardInput.Close();
                await process.WaitForExitAsync(timeout.Token);
                var json = await output; await errors;
                if (process.ExitCode != 0) throw new InvalidOperationException("Speech worker failed.");
                using var result = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
                var text = result.RootElement.GetProperty("text").GetString()?.Trim() ?? "";
                if (text.Length < 2) throw new AiInterviewException("Chưa nghe rõ câu trả lời. Bạn ghi âm lại gần micro hơn nhé.", 422);
                if (text.Length > 4000) throw new InvalidDataException("Speech output too long.");
                return text;
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }
                try { await Task.WhenAll(output, errors); } catch { }
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new AiInterviewException("Nhận giọng nói lâu hơn dự kiến. Bạn thử ghi đoạn ngắn hơn hoặc nhập câu trả lời nhé.", 504); }
        catch (OperationCanceledException) { throw; }
        catch (AiInterviewException) { throw; }
        catch (Exception ex)
        {
            // Do not log the recording, transcript or worker exception (which can contain private paths).
            logger.LogWarning("Nhận giọng nói không sẵn sàng ({ErrorType}).", ex.GetType().Name);
            throw new AiInterviewException("Nhận giọng nói hiện chưa sẵn sàng. Bạn có thể nhập câu trả lời hoặc thử lại sau.");
        }
    }, token);

    private static async Task<string> DocKetQuaAsync(StreamReader reader, CancellationToken token)
    {
        var text = new StringBuilder(); var buffer = new char[2048]; int count;
        while ((count = await reader.ReadAsync(buffer, token)) > 0)
        {
            if (text.Length + count > 32768) throw new InvalidDataException("Speech output too long.");
            text.Append(buffer, 0, count);
        }
        return text.ToString();
    }

    private static async Task DocBoThongBaoAsync(StreamReader reader, CancellationToken token)
    {
        // Drain stderr to avoid a blocked child, without retaining private diagnostics.
        var buffer = new char[2048];
        while (await reader.ReadAsync(buffer, token) > 0) { }
    }

    public static void KiemTraAmThanh(byte[] data)
    {
        // A deliberately narrow format: browser-generated PCM16, mono, 16kHz, canonical 44-byte WAV header.
        if (data.Length is < 16044 or > MaxAudioBytes ||
            Encoding.ASCII.GetString(data, 0, 4) != "RIFF" || Encoding.ASCII.GetString(data, 8, 8) != "WAVEfmt " ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) != data.Length - 8 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16)) != 16 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(20)) != 1 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(22)) != 1 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(24)) != 16000 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(28)) != 32000 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(32)) != 2 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(34)) != 16 ||
            Encoding.ASCII.GetString(data, 36, 4) != "data" ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(40)) != data.Length - 44 || data.Length % 2 != 0)
            throw new AiInterviewException("Bản ghi âm chưa hợp lệ. Bạn ghi âm lại nhé.", 400);
        double energy = 0;
        for (var index = 44; index < data.Length; index += 2)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(index)) / 32768.0;
            energy += sample * sample;
        }
        if (Math.Sqrt(energy / ((data.Length - 44) / 2)) < 0.003)
            throw new AiInterviewException("Bản ghi gần như im lặng. Bạn kiểm tra micro rồi thử lại nhé.", 422);
    }
}
