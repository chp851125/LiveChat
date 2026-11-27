using LiveChat.Models;
using LiveChat.Repositories;

namespace LiveChat.Services
{
    public class ChannelService : IChannelService
    {
        public const int MaxNameLength = 50;

        // 「先查再寫」的規則（重名、重複加入、R3 唯一私訊）都在同一把鎖內完成
        private static readonly Lock WriteLock = new();

        private readonly IRepository<Channel> _channels;
        private readonly IRepository<ChannelMember> _members;
        private readonly IRepository<User> _users;

        public ChannelService(
            IRepository<Channel> channels,
            IRepository<ChannelMember> members,
            IRepository<User> users)
        {
            _channels = channels;
            _members = members;
            _users = users;
        }

        public IReadOnlyList<Channel> GetJoinedChannels(Guid userId)
        {
            var joinedIds = JoinedChannelIds(userId);
            return _channels
                .Find(x => x.Type != ChannelType.Direct && joinedIds.Contains(x.Id))
                .OrderBy(x => x.IsArchived)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public IReadOnlyList<Channel> GetJoinableChannels(Guid userId)
        {
            var joinedIds = JoinedChannelIds(userId);
            return _channels
                .Find(x => x.Type == ChannelType.Public && !x.IsArchived && !joinedIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToList();
        }

        public IReadOnlyList<DirectConversation> GetDirectConversations(Guid userId)
        {
            var joinedIds = JoinedChannelIds(userId);
            var users = _users.GetAll().ToDictionary(x => x.Id);
            var others = _members
                .Find(x => x.UserId != userId && joinedIds.Contains(x.ChannelId))
                .ToLookup(x => x.ChannelId, x => x.UserId);

            return _channels
                .Find(x => x.Type == ChannelType.Direct && joinedIds.Contains(x.Id))
                .SelectMany(channel => others[channel.Id]
                    .Where(users.ContainsKey)
                    .Select(id => new DirectConversation(channel, users[id])))
                .OrderBy(x => x.OtherUser.DisplayName)
                .ToList();
        }

        public IReadOnlyList<User> GetDirectCandidates(Guid userId) =>
            _users.Find(x => x.Id != userId).OrderBy(x => x.UserName).ToList();

        public Channel GetAccessibleChannel(Guid channelId, Guid userId)
        {
            var channel = _channels.GetById(channelId);
            if (channel is null || (channel.Type != ChannelType.Public && !IsMember(channelId, userId)))
            {
                throw new ChannelAccessDeniedException();
            }

            return channel;
        }

        public IReadOnlyList<User> GetMembers(Guid channelId)
        {
            var memberIds = MemberIds(channelId);
            return _users.Find(x => memberIds.Contains(x.Id)).OrderBy(x => x.UserName).ToList();
        }

        public IReadOnlyList<User> GetNonMembers(Guid channelId)
        {
            var memberIds = MemberIds(channelId);
            return _users.Find(x => !memberIds.Contains(x.Id)).OrderBy(x => x.UserName).ToList();
        }

        public Channel Create(string name, ChannelType type, Guid creatorId)
        {
            if (type is not (ChannelType.Public or ChannelType.Private))
            {
                throw new BusinessRuleException("只能建立公開或私人頻道。");
            }

            var trimmed = name.Trim();
            if (trimmed.Length is < 1 or > MaxNameLength)
            {
                throw new BusinessRuleException($"頻道名稱需為 1～{MaxNameLength} 字。");
            }

            lock (WriteLock)
            {
                RequireUser(creatorId);

                // 只比對公開頻道，避免透露私人頻道是否存在
                if (type == ChannelType.Public && _channels.Find(x => x.Type == ChannelType.Public
                    && string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase)).Count > 0)
                {
                    throw new BusinessRuleException("已有同名的公開頻道。");
                }

                var channel = new Channel { Name = trimmed, Type = type, CreatedByUserId = creatorId };
                _channels.Add(channel);
                AddMember(channel.Id, creatorId);
                return channel;
            }
        }

        public void Join(Guid channelId, Guid userId)
        {
            lock (WriteLock)
            {
                var channel = GetAccessibleChannel(channelId, userId);
                if (channel.Type != ChannelType.Public)
                {
                    throw new BusinessRuleException("只有公開頻道可以自行加入。");
                }

                RequireNotArchived(channel);
                RequireUser(userId);

                if (IsMember(channelId, userId))
                {
                    throw new BusinessRuleException("你已經是這個頻道的成員。");
                }

                AddMember(channelId, userId);
            }
        }

        public void Leave(Guid channelId, Guid userId)
        {
            lock (WriteLock)
            {
                var channel = GetAccessibleChannel(channelId, userId);
                if (channel.Type != ChannelType.Public)
                {
                    throw new BusinessRuleException("只有公開頻道可以自行離開。");
                }

                RequireNotArchived(channel);

                if (!RemoveMember(channelId, userId))
                {
                    throw new BusinessRuleException("你不是這個頻道的成員。");
                }
            }
        }

        public void Invite(Guid channelId, Guid actorId, Guid targetUserId)
        {
            lock (WriteLock)
            {
                RequireManageable(channelId, actorId);
                RequireUser(targetUserId);

                if (IsMember(channelId, targetUserId))
                {
                    throw new BusinessRuleException("對方已經是這個頻道的成員。");
                }

                AddMember(channelId, targetUserId);
            }
        }

        public void Remove(Guid channelId, Guid actorId, Guid targetUserId)
        {
            lock (WriteLock)
            {
                var channel = RequireManageable(channelId, actorId);
                if (targetUserId == channel.CreatedByUserId)
                {
                    throw new BusinessRuleException("頻道建立者不能被移除。");
                }

                if (!RemoveMember(channelId, targetUserId))
                {
                    throw new BusinessRuleException("對方不是這個頻道的成員。");
                }
            }
        }

        public Channel GetOrCreateDirect(Guid userId, Guid otherUserId)
        {
            if (userId == otherUserId)
            {
                throw new BusinessRuleException("不能和自己私訊。");
            }

            lock (WriteLock)
            {
                var user = RequireUser(userId);
                var other = RequireUser(otherUserId);

                var shared = JoinedChannelIds(userId);
                shared.IntersectWith(JoinedChannelIds(otherUserId));

                var existing = _channels
                    .Find(x => x.Type == ChannelType.Direct && shared.Contains(x.Id))
                    .FirstOrDefault();
                if (existing is not null)
                {
                    return existing;
                }

                var channel = new Channel
                {
                    Name = $"{user.UserName}, {other.UserName}",
                    Type = ChannelType.Direct,
                    CreatedByUserId = userId
                };
                _channels.Add(channel);
                AddMember(channel.Id, userId);
                AddMember(channel.Id, otherUserId);
                return channel;
            }
        }

        /// <summary>R2：只有私人頻道的建立者能邀請或移除成員。</summary>
        private Channel RequireManageable(Guid channelId, Guid actorId)
        {
            var channel = GetAccessibleChannel(channelId, actorId);
            if (channel.Type != ChannelType.Private)
            {
                throw new BusinessRuleException("只有私人頻道可以邀請或移除成員。");
            }

            if (channel.CreatedByUserId != actorId)
            {
                throw new BusinessRuleException("只有頻道建立者可以邀請或移除成員。");
            }

            RequireNotArchived(channel);
            return channel;
        }

        private static void RequireNotArchived(Channel channel)
        {
            if (channel.IsArchived)
            {
                throw new BusinessRuleException("頻道已封存，無法異動成員。");
            }
        }

        private User RequireUser(Guid userId) =>
            _users.GetById(userId) ?? throw new BusinessRuleException("找不到使用者。");

        private bool IsMember(Guid channelId, Guid userId) =>
            _members.Find(x => x.ChannelId == channelId && x.UserId == userId).Count > 0;

        private HashSet<Guid> JoinedChannelIds(Guid userId) =>
            _members.Find(x => x.UserId == userId).Select(x => x.ChannelId).ToHashSet();

        private HashSet<Guid> MemberIds(Guid channelId) =>
            _members.Find(x => x.ChannelId == channelId).Select(x => x.UserId).ToHashSet();

        private void AddMember(Guid channelId, Guid userId) =>
            _members.Add(new ChannelMember { ChannelId = channelId, UserId = userId });

        private bool RemoveMember(Guid channelId, Guid userId)
        {
            var matches = _members.Find(x => x.ChannelId == channelId && x.UserId == userId);
            foreach (var member in matches)
            {
                _members.Delete(member.Id);
            }

            return matches.Count > 0;
        }
    }
}
