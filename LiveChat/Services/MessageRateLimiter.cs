namespace LiveChat.Services
{
    /// <summary>記憶體內的滑動視窗，需以 Singleton 註冊；程式重啟後歸零。</summary>
    public class MessageRateLimiter : IMessageRateLimiter
    {
        public const int Limit = 5;
        public static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

        private readonly Lock _lock = new();

        // 每位使用者視窗內的發送時間（舊到新），最多 Limit 筆
        private readonly Dictionary<Guid, Queue<DateTime>> _sentTimes = new();

        public bool TryAcquire(Guid userId, out TimeSpan retryAfter)
        {
            lock (_lock)
            {
                var utcNow = DateTime.UtcNow;
                if (!_sentTimes.TryGetValue(userId, out var times))
                {
                    times = new Queue<DateTime>();
                    _sentTimes[userId] = times;
                }

                while (times.Count > 0 && utcNow - times.Peek() >= Window)
                {
                    times.Dequeue();
                }

                if (times.Count >= Limit)
                {
                    retryAfter = times.Peek() + Window - utcNow;
                    return false;
                }

                times.Enqueue(utcNow);
                retryAfter = TimeSpan.Zero;
                return true;
            }
        }
    }
}
