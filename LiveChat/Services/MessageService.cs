using LiveChat.Models;
using LiveChat.Repositories;

namespace LiveChat.Services
{
    public class MessageService : IMessageService
    {
        public const int MaxContentLength = 1000;
        public const int PageSize = 50;
        public static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(5);

        // 編輯與刪除是「先查再寫」，避免互相覆蓋（例如編輯蓋掉剛完成的刪除）
        private static readonly Lock WriteLock = new();

        private readonly IRepository<Message> _messages;
        private readonly IRepository<ChannelMember> _members;
        private readonly IRepository<User> _users;
        private readonly IRepository<Mute> _mutes;
        private readonly IChannelService _channelService;
        private readonly IMessageRateLimiter _rateLimiter;

        public MessageService(
            IRepository<Message> messages,
            IRepository<ChannelMember> members,
            IRepository<User> users,
            IRepository<Mute> mutes,
            IChannelService channelService,
            IMessageRateLimiter rateLimiter)
        {
            _messages = messages;
            _members = members;
            _users = users;
            _mutes = mutes;
            _channelService = channelService;
            _rateLimiter = rateLimiter;
        }

        public MessagePage GetMessages(Guid channelId, Guid userId, Guid? beforeMessageId = null)
        {
            var channel = RequireMemberChannel(channelId, userId);
            var all = _messages
                .Find(x => x.ChannelId == channelId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();

            // 找不到指定訊息時顯示最新一頁
            var end = beforeMessageId is null ? -1 : all.FindIndex(x => x.Id == beforeMessageId);
            if (end < 0)
            {
                end = all.Count;
            }

            var start = Math.Max(0, end - PageSize);
            var users = _users.GetAll().ToDictionary(x => x.Id);
            var isAdmin = users.TryGetValue(userId, out var viewer) && viewer.Role == UserRole.Admin;
            var muteUntil = ActiveMuteUntil(userId);
            var utcNow = DateTime.UtcNow;

            var entries = all
                .GetRange(start, end - start)
                .Select(x => new MessageEntry(
                    x.Id,
                    x.SenderId,
                    users.TryGetValue(x.SenderId, out var sender) ? sender.DisplayName : "未知使用者",
                    x.IsDeleted ? string.Empty : x.Content,
                    x.CreatedAt,
                    x.EditedAt is not null,
                    x.IsDeleted,
                    FindEditBlock(x, channel, userId, muteUntil, utcNow) is null,
                    FindDeleteBlock(x, channel, userId, isAdmin) is null))
                .ToList();

            return new MessagePage(entries, start > 0);
        }

        public string? GetSendRestriction(Guid channelId, Guid userId)
        {
            var channel = RequireMemberChannel(channelId, userId);
            return FindSendBlock(channel, ActiveMuteUntil(userId));
        }

        public Message Send(Guid channelId, Guid userId, string content)
        {
            lock (WriteLock)
            {
                var channel = RequireMemberChannel(channelId, userId);
                Require(FindSendBlock(channel, ActiveMuteUntil(userId)));
                var normalized = NormalizeContent(content);

                // 限流放最後，被其他規則拒絕的嘗試不佔額度
                if (!_rateLimiter.TryAcquire(userId, out var retryAfter))
                {
                    throw new BusinessRuleException(
                        $"發送太頻繁：{MessageRateLimiter.Window.TotalSeconds:0} 秒內最多 {MessageRateLimiter.Limit} 則，" +
                        $"請在 {Math.Ceiling(retryAfter.TotalSeconds):0} 秒後再試。");
                }

                var message = new Message
                {
                    ChannelId = channelId,
                    SenderId = userId,
                    Content = normalized,
                    CreatedAt = DateTime.UtcNow
                };
                _messages.Add(message);
                return message;
            }
        }

        public Message Edit(Guid messageId, Guid userId, string content)
        {
            lock (WriteLock)
            {
                var message = RequireMessage(messageId);
                var channel = RequireMemberChannel(message.ChannelId, userId);
                var utcNow = DateTime.UtcNow;
                Require(FindEditBlock(message, channel, userId, ActiveMuteUntil(userId), utcNow));

                message.Content = NormalizeContent(content);
                message.EditedAt = utcNow;
                Save(message);
                return message;
            }
        }

        public Message Delete(Guid messageId, Guid userId)
        {
            lock (WriteLock)
            {
                var message = RequireMessage(messageId);
                var channel = RequireMemberChannel(message.ChannelId, userId);
                var isAdmin = _users.GetById(userId)?.Role == UserRole.Admin;
                Require(FindDeleteBlock(message, channel, userId, isAdmin));

                message.IsDeleted = true;
                Save(message);
                return message;
            }
        }

        /// <summary>R4：去除前後空白後 1～1000 字。換行統一為 \n，一個換行算一字。</summary>
        private static string NormalizeContent(string content)
        {
            var normalized = content.ReplaceLineEndings("\n").Trim();
            if (normalized.Length == 0)
            {
                throw new BusinessRuleException("訊息內容不能是空白。");
            }

            if (normalized.Length > MaxContentLength)
            {
                throw new BusinessRuleException(
                    $"訊息內容最多 {MaxContentLength} 字，目前 {normalized.Length} 字。");
            }

            return normalized;
        }

        /// <summary>R7、R8。</summary>
        private static string? FindSendBlock(Channel channel, DateTime? muteUntil)
        {
            if (channel.IsArchived)
            {
                return "頻道已封存，目前唯讀。";
            }

            return muteUntil is null ? null : MutedReason(muteUntil.Value, "發送訊息");
        }

        /// <summary>R5、R7、R8；回傳不能編輯的原因。</summary>
        private static string? FindEditBlock(
            Message message, Channel channel, Guid userId, DateTime? muteUntil, DateTime utcNow)
        {
            if (message.SenderId != userId)
            {
                return "只能編輯自己的訊息。";
            }

            if (message.IsDeleted)
            {
                return "已刪除的訊息不能編輯。";
            }

            if (channel.IsArchived)
            {
                return "頻道已封存，目前唯讀。";
            }

            if (muteUntil is not null)
            {
                return MutedReason(muteUntil.Value, "編輯訊息");
            }

            return utcNow - message.CreatedAt < EditWindow
                ? null
                : $"發送超過 {EditWindow.TotalMinutes:0} 分鐘的訊息不能編輯。";
        }

        /// <summary>R6、R8；回傳不能刪除的原因。禁言中仍可刪除。</summary>
        private static string? FindDeleteBlock(Message message, Channel channel, Guid userId, bool isAdmin)
        {
            if (message.SenderId != userId && !isAdmin)
            {
                return "只能刪除自己的訊息。";
            }

            if (message.IsDeleted)
            {
                return "此訊息已刪除。";
            }

            return channel.IsArchived ? "頻道已封存，目前唯讀。" : null;
        }

        private static string MutedReason(DateTime until, string action) =>
            $"你已被禁言至 {until.ToLocalTime():yyyy/MM/dd HH:mm}，期間不能{action}。";

        private static void Require(string? block)
        {
            if (block is not null)
            {
                throw new BusinessRuleException(block);
            }
        }

        /// <summary>R7：Until 大於現在才算禁言中；有多筆時取最晚的。</summary>
        private DateTime? ActiveMuteUntil(Guid userId)
        {
            var utcNow = DateTime.UtcNow;
            return _mutes
                .Find(x => x.UserId == userId && x.Until > utcNow)
                .Max(x => (DateTime?)x.Until);
        }

        /// <summary>讀寫訊息都必須是頻道成員（公開頻道也一樣，R1）。</summary>
        private Channel RequireMemberChannel(Guid channelId, Guid userId)
        {
            var channel = _channelService.GetAccessibleChannel(channelId, userId);
            if (_members.Find(x => x.ChannelId == channelId && x.UserId == userId).Count == 0)
            {
                throw new BusinessRuleException("加入頻道後才能讀寫訊息。");
            }

            return channel;
        }

        private Message RequireMessage(Guid messageId) =>
            _messages.GetById(messageId) ?? throw new BusinessRuleException("找不到訊息。");

        private void Save(Message message)
        {
            if (!_messages.Update(message))
            {
                throw new BusinessRuleException("找不到訊息。");
            }
        }
    }
}
