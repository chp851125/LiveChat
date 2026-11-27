using LiveChat.Models;

namespace LiveChat.Services
{
    /// <summary>
    /// 訊息規則（R4～R9）。Controller 與 Hub 共用。
    /// 規則不成立丟 BusinessRuleException；無權進入頻道丟 ChannelAccessDeniedException。
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// 取得最近一頁訊息（舊到新）；指定 beforeMessageId 時取該則之前的一頁。僅限成員。
        /// </summary>
        MessagePage GetMessages(Guid channelId, Guid userId, Guid? beforeMessageId = null);

        /// <summary>目前不能在此頻道發送的原因（封存、禁言）；可以發送回傳 null。不含限流與內容檢查。</summary>
        string? GetSendRestriction(Guid channelId, Guid userId);

        Message Send(Guid channelId, Guid userId, string content);

        Message Edit(Guid messageId, Guid userId, string content);

        /// <summary>軟刪除。</summary>
        Message Delete(Guid messageId, Guid userId);
    }
}
