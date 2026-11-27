using LiveChat.Models;

namespace LiveChat.ViewModels
{
    /// <summary>頻道頁與成員管理頁共用。</summary>
    public class ChannelViewModel
    {
        public required Channel Channel { get; init; }

        /// <summary>顯示用標題；私訊為對方名稱。</summary>
        public required string Title { get; init; }

        public IReadOnlyList<User> Members { get; init; } = Array.Empty<User>();

        public bool IsMember { get; init; }

        public bool CanManageMembers { get; init; }

        public IReadOnlyList<User> InviteCandidates { get; init; } = Array.Empty<User>();
    }
}
