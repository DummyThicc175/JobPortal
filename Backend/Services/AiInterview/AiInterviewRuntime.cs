namespace JobPortal.Services;

public sealed class AiInterviewException(string message, int statusCode = 503) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Share the memory budget between speech recognition and language generation.
public sealed class AiInterviewRuntime : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<T> ChayDocQuyenAsync<T>(Func<Task<T>> congViec, CancellationToken token)
    {
        if (!await _gate.WaitAsync(TimeSpan.FromSeconds(3), token))
            throw new AiInterviewException("AI đang xử lý phiên khác. Bạn thử lại sau ít giây nhé.", 429);
        try { return await congViec(); }
        finally { _gate.Release(); }
    }
    public void Dispose() => _gate.Dispose();
}
