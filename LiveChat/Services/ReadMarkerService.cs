using LiveChat.Models;
using LiveChat.Repositories;

namespace LiveChat.Services
{
    public class ReadMarkerService : IReadMarkerService
    {
        // 避免同一人同一頻道重複建立標記
        private static readonly Lock WriteLock = new();

        private readonly IRepository<ReadMarker> _readMarkers;
        private readonly IRepository<ChannelMember> _members;
        private readonly IRepository<Message> _messages;

        public ReadMarkerService(
            IRepository<ReadMarker> readMarkers,
            IRepository<ChannelMember> members,
            IRepository<Message> messages)
        {
            _readMarkers = readMarkers;
            _members = members;
            _messages = messages;
        }

        /// <summary>R10：非自己發送、未刪除、晚於 LastReadAt；沒有標記視為全部未讀。</summary>
        public static bool IsUnread(Message message, Guid userId, DateTime? lastReadAt) =>
            message.SenderId != userId
            && !message.IsDeleted
            && (lastReadAt is null || message.CreatedAt > lastReadAt);

        public IReadOnlyDictionary<Guid, int> GetUnreadCounts(Guid userId)
        {
            var channelIds = _members.Find(x => x.UserId == userId).Select(x => x.ChannelId).ToHashSet();
            var lastReads = _readMarkers
                .Find(x => x.UserId == userId)
                .GroupBy(x => x.ChannelId)
                .ToDictionary(x => x.Key, x => x.Max(m => m.LastReadAt));
            var messages = _messages
                .Find(x => channelIds.Contains(x.ChannelId))
                .ToLookup(x => x.ChannelId);

            return channelIds.ToDictionary(
                id => id,
                id =>
                {
                    DateTime? lastReadAt = lastReads.TryGetValue(id, out var value) ? value : null;
                    return messages[id].Count(x => IsUnread(x, userId, lastReadAt));
                });
        }

        public void MarkAsRead(Guid channelId, Guid userId)
        {
            lock (WriteLock)
            {
                if (_members.Find(x => x.ChannelId == channelId && x.UserId == userId).Count == 0)
                {
                    return;
                }

                var utcNow = DateTime.UtcNow;
                var marker = _readMarkers
                    .Find(x => x.ChannelId == channelId && x.UserId == userId)
                    .FirstOrDefault();
                if (marker is null)
                {
                    _readMarkers.Add(new ReadMarker { UserId = userId, ChannelId = channelId, LastReadAt = utcNow });
                    return;
                }

                marker.LastReadAt = utcNow;
                _readMarkers.Update(marker);
            }
        }
    }
}
