using LiveChat.Models;
using LiveChat.Services;

namespace LiveChat.ViewModels
{
    /// <summary>頻道頁與成員管理頁共用。</summary>
    public record ChannelViewModel
    {
        public required Channel Channel { get; init; }

        /// <summary>顯示用標題；私訊為對方名稱。</summary>
        public required string Title { get; init; }

        public IReadOnlyList<User> Members { get; init; } = Array.Empty<User>();

        public bool IsMember { get; init; }

        public bool CanManageMembers { get; init; }

        public IReadOnlyList<User> InviteCandidates { get; init; } = Array.Empty<User>();

        // 以下只有聊天室頁使用

        /// <summary>非成員為 null（R1：看不到訊息內容）。</summary>
        public MessagePage? Messages { get; init; }

        /// <summary>不能發送的原因；可以發送為 null。</summary>
        public string? SendRestriction { get; init; }

        /// <summary>目前這頁是「載入更早訊息」的分頁時，其游標訊息 Id。</summary>
        public Guid? Before { get; init; }

        /// <summary>送出失敗時要保留的輸入內容。</summary>
        public string? Draft { get; init; }

        /// <summary>編輯失敗的訊息 Id；Draft 屬於該則的編輯框。</summary>
        public Guid? EditingMessageId { get; init; }
    }
}
