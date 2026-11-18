using System.Collections.Concurrent;
using System.Threading.Channels;

namespace JobPortal.Services;

public sealed record InterviewSpeechJobStatus(string Status, string? Text, string? Message);

// Audio stays in memory. Each HTTP request is short, so slow local inference also works through a tunnel.
public sealed class InterviewSpeechJobs(IServiceScopeFactory scopes) : BackgroundService
{
    private sealed class CongViec(string owner, Guid session, byte[] audio)
    {
        public string Owner { get; } = owner;
        public Guid Session { get; } = session;
        public byte[]? Audio { get; set; } = audio;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public InterviewSpeechJobStatus State { get; set; } = new("pending", null, null);
    }
    private readonly ConcurrentDictionary<Guid, CongViec> _jobs = new();
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(1);
    private int _active;

    public Guid ThemCongViec(string userId, Guid sessionId, byte[] audio)
    {
        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
            throw new AiInterviewException("Nhận giọng nói đang xử lý một bản ghi khác. Bạn thử lại sau nhé.", 429);
        foreach (var pair in _jobs)
            if (pair.Value.CreatedAt < DateTime.UtcNow.AddMinutes(-20)) _jobs.TryRemove(pair.Key, out _);
        var id = Guid.NewGuid();
        _jobs[id] = new(userId, sessionId, audio);
        if (!_queue.Writer.TryWrite(id))
        {
            _jobs.TryRemove(id, out _); Interlocked.Exchange(ref _active, 0);
            throw new AiInterviewException("Nhận giọng nói đang bận. Bạn thử lại nhé.", 429);
        }
        return id;
    }

    public InterviewSpeechJobStatus? LayTrangThai(Guid id, string userId, Guid sessionId)
    {
        if (!_jobs.TryGetValue(id, out var job) || job.Owner != userId || job.Session != sessionId ||
            job.CreatedAt < DateTime.UtcNow.AddMinutes(-20)) return null;
        lock (job) return job.State;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_jobs.TryGetValue(id, out var job)) { Interlocked.Exchange(ref _active, 0); continue; }
            try
            {
                lock (job) job.State = new("running", null, null);
                using var scope = scopes.CreateScope();
                await using var audio = new MemoryStream(job.Audio!, writable: false);
                var text = await scope.ServiceProvider.GetRequiredService<IInterviewSpeechService>().ChuyenThanhVanBanAsync(audio, stoppingToken);
                lock (job) job.State = new("completed", text, null);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (AiInterviewException ex) { lock (job) job.State = new("failed", null, ex.Message); }
            catch (Exception) { lock (job) job.State = new("failed", null, "Chưa nhận được giọng nói. Bạn thử lại nhé."); }
            finally { job.Audio = null; Interlocked.Exchange(ref _active, 0); }
        }
    }
}
