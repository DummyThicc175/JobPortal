using Microsoft.Extensions.Caching.Memory;

namespace JobPortal.Services;

public sealed class ChatMessageThrottle(IMemoryCache cache)
{
    private readonly object _gate = new();
    public bool TrySend(string userId)
    {
        lock (_gate)
        {
            var key = "chat-throttle:" + userId;
            if (!cache.TryGetValue(key, out Counter? counter))
                cache.Set(key, counter = new Counter(), TimeSpan.FromSeconds(10));
            return ++counter!.Count <= 10;
        }
    }
    private sealed class Counter { public int Count; }
}
