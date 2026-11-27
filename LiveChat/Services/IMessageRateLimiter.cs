namespace LiveChat.Services
{
    /// <summary>R9：每位使用者的發送限流。</summary>
    public interface IMessageRateLimiter
    {
        /// <summary>未超過上限時記錄一次並回傳 true；否則回傳 false 與還需等待的時間。</summary>
        bool TryAcquire(Guid userId, out TimeSpan retryAfter);
    }
}
