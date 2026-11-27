using LiveChat.Models;
using LiveChat.Services;

namespace LiveChat.ViewModels
{
    /// <summary>聊天室左側的頻道清單。</summary>
    public class ChannelSidebarViewModel
    {
        public IReadOnlyList<Channel> Joined { get; init; } = Array.Empty<Channel>();

        public IReadOnlyList<DirectConversation> Directs { get; init; } = Array.Empty<DirectConversation>();

        public IReadOnlyDictionary<Guid, int> UnreadCounts { get; init; } = new Dictionary<Guid, int>();

        public Guid CurrentChannelId { get; init; }
    }
}
