using LiveChat.Models;

namespace LiveChat.Services
{
    /// <summary>頻道與成員規則（R1～R3）。規則不成立丟 BusinessRuleException；無權進入丟 ChannelAccessDeniedException。</summary>
    public interface IChannelService
    {
        /// <summary>我加入的公開與私人頻道（含已封存）。</summary>
        IReadOnlyList<Channel> GetJoinedChannels(Guid userId);

        /// <summary>尚未加入且未封存的公開頻道。</summary>
        IReadOnlyList<Channel> GetJoinableChannels(Guid userId);

        IReadOnlyList<DirectConversation> GetDirectConversations(Guid userId);

        /// <summary>可開始私訊的對象（自己以外的所有人）。</summary>
        IReadOnlyList<User> GetDirectCandidates(Guid userId);

        /// <summary>公開頻道任何人可取得；私人頻道與私訊僅限成員。</summary>
        Channel GetAccessibleChannel(Guid channelId, Guid userId);

        /// <summary>呼叫端須先用 GetAccessibleChannel 確認權限。</summary>
        IReadOnlyList<User> GetMembers(Guid channelId);

        /// <summary>呼叫端須先用 GetAccessibleChannel 確認權限。</summary>
        IReadOnlyList<User> GetNonMembers(Guid channelId);

        Channel Create(string name, ChannelType type, Guid creatorId);

        void Join(Guid channelId, Guid userId);

        void Leave(Guid channelId, Guid userId);

        void Invite(Guid channelId, Guid actorId, Guid targetUserId);

        void Remove(Guid channelId, Guid actorId, Guid targetUserId);

        /// <summary>兩人之間已有私訊就回傳既有的，否則建立。</summary>
        Channel GetOrCreateDirect(Guid userId, Guid otherUserId);
    }
}
