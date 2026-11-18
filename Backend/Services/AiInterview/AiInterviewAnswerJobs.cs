using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using JobPortal.ViewModels.Candidate;

namespace JobPortal.Services;

public sealed record AiInterviewAnswerJobStatus(string Status, bool? Completed, string? Message, int? StatusCode);

// HTTP only enqueues/polls. Model calls and the atomic save run in their own scope.
public sealed class AiInterviewAnswerJobs(IServiceScopeFactory scopes, ILogger<AiInterviewAnswerJobs> logger) : BackgroundService
{
    private sealed class CongViec(string owner, Guid session, int number, string answer)
    {
        public string Owner { get; } = owner;
        public Guid Session { get; } = session;
        public int Number { get; } = number;
        public string? Answer { get; set; } = answer;
        public string Fingerprint { get; } = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(answer)));
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public AiInterviewAnswerJobStatus State { get; set; } = new("pending", null, null, null);
    }

    private readonly object _sync = new();
    private readonly Dictionary<Guid, CongViec> _jobs = new();
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(1);
    private Guid? _activeId;

    public Guid ThemCongViec(string userId, Guid sessionId, AiInterviewAnswerInput input)
    {
        var answer = input.Answer.Trim();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(answer)));
        lock (_sync)
        {
            foreach (var id in _jobs.Where(p => p.Key != _activeId && p.Value.CreatedAt < DateTime.UtcNow.AddMinutes(-20)).Select(p => p.Key).ToArray())
                _jobs.Remove(id);
            var existing = _jobs.FirstOrDefault(p => p.Value.Owner == userId && p.Value.Session == sessionId &&
                p.Value.Number == input.Number && p.Value.State.Status != "failed");
            if (existing.Value != null)
            {
                if (existing.Value.Fingerprint == fingerprint) return existing.Key;
                throw new AiInterviewException("Câu này đang được xử lý hoặc đã được gửi ở trang khác. Bạn tải lại để tiếp tục nhé.", 409);
            }
            if (_activeId != null)
                throw new AiInterviewException("AI đang xử lý câu trả lời khác. Nội dung của bạn vẫn còn để gửi lại sau nhé.", 429);
            var jobId = Guid.NewGuid();
            _jobs[jobId] = new(userId, sessionId, input.Number, answer);
            _activeId = jobId;
            if (!_queue.Writer.TryWrite(jobId))
            {
                _jobs.Remove(jobId); _activeId = null;
                throw new AiInterviewException("AI đang bận. Bạn thử gửi lại sau nhé.", 429);
            }
            return jobId;
        }
    }

    public AiInterviewAnswerJobStatus? LayTrangThai(Guid jobId, string userId, Guid sessionId)
    {
        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var job) || job.Owner != userId || job.Session != sessionId ||
                job.CreatedAt < DateTime.UtcNow.AddMinutes(-20)) return null;
            return job.State;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            CongViec job;
            lock (_sync) { job = _jobs[jobId]; job.State = new("running", null, null, null); }
            var clock = Stopwatch.StartNew();
            AiInterviewAnswerJobStatus? finalState = null;
            logger.LogInformation("Bắt đầu chấm phỏng vấn {JobId}.", jobId);
            try
            {
                using var scope = scopes.CreateScope();
                var completed = await scope.ServiceProvider.GetRequiredService<AiInterviewService>().TraLoiAsync(
                    job.Session, job.Owner, new AiInterviewAnswerInput { Number = job.Number, Answer = job.Answer! }, stoppingToken);
                finalState = new("completed", completed, null, null);
                logger.LogInformation("Đã chấm phỏng vấn {JobId} sau {ElapsedSeconds:F1} giây.", jobId, clock.Elapsed.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (AiInterviewException ex)
            {
                finalState = new("failed", null, ex.Message, ex.StatusCode);
                logger.LogWarning("Chấm phỏng vấn {JobId} thất bại, mã {StatusCode}, sau {ElapsedSeconds:F1} giây.", jobId, ex.StatusCode, clock.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                // Log type and timing only: no answer, CV, prompt, keys or private SQL diagnostics.
                finalState = new("failed", null, "Chưa chấm được câu trả lời do lỗi máy chủ. Nội dung vẫn được giữ để bạn thử lại.", 500);
                logger.LogWarning("Chấm phỏng vấn {JobId} gặp {ErrorType} sau {ElapsedSeconds:F1} giây.", jobId, ex.GetType().Name, clock.Elapsed.TotalSeconds);
            }
            finally
            {
                lock (_sync)
                {
                    job.State = finalState ?? new("failed", null, "Máy chủ đã dừng xử lý. Bạn tải lại trang để kiểm tra câu trả lời nhé.", 503);
                    job.Answer = null; _activeId = null;
                }
            }
        }
    }
}
