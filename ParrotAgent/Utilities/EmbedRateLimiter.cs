using System.Collections.Concurrent;

namespace ParrotAgent.Utilities
{
    public static class EmbedRateLimiter
    {
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> Hits = new();

        public static bool IsAllowed(string key, int limit, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var queue = Hits.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (queue)
            {
                while (queue.Count > 0 && now - queue.Peek() > window)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= limit)
                {
                    return false;
                }

                queue.Enqueue(now);
                return true;
            }
        }
    }
}
