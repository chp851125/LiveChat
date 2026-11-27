using LiveChat.Models;

namespace LiveChat.DevTools
{
    /// <summary>檢查測試資料的關聯完整性、各情況筆數與相對日期。</summary>
    public class SeedValidator
    {
        private const int ExpectedCount = 3;
        private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DateRange = TimeSpan.FromDays(30);

        public IReadOnlyList<SeedCheck> Validate(SeedDataSet data, DateTime utcNow)
        {
            var checks = new List<SeedCheck>();
            CheckIds(checks, data);
            CheckRelations(checks, data);
            CheckCounts(checks, data, utcNow);
            CheckDates(checks, data, utcNow);
            return checks;
        }

        private static void CheckIds(List<SeedCheck> checks, SeedDataSet data)
        {
            void Unique(string name, IEnumerable<IEntity> items)
            {
                var ids = items.Select(x => x.Id).ToList();
                var passed = ids.All(x => x != Guid.Empty) && ids.Distinct().Count() == ids.Count;
                Add(checks, $"Id 不為空且不重複：{name}", passed, $"{ids.Count} 筆");
            }

            Unique("User", data.Users);
            Unique("Channel", data.Channels);
            Unique("ChannelMember", data.ChannelMembers);
            Unique("Message", data.Messages);
            Unique("ReadMarker", data.ReadMarkers);
            Unique("Mute", data.Mutes);
            Unique("AuditLog", data.AuditLogs);
        }

        private static void CheckRelations(List<SeedCheck> checks, SeedDataSet data)
        {
            var userIds = data.Users.Select(x => x.Id).ToHashSet();
            var channelIds = data.Channels.Select(x => x.Id).ToHashSet();
            var memberships = data.ChannelMembers.Select(x => (x.ChannelId, x.UserId)).ToHashSet();

            void None(string name, int brokenCount) =>
                Add(checks, name, brokenCount == 0, $"違反 {brokenCount} 筆");

            None("帳號不重複",
                data.Users.Count - data.Users.Select(x => x.UserName.ToLowerInvariant()).Distinct().Count());
            None("頻道建立者存在且為成員",
                data.Channels.Count(x => !userIds.Contains(x.CreatedByUserId)
                    || !memberships.Contains((x.Id, x.CreatedByUserId))));
            None("頻道成員指向存在的頻道與使用者",
                data.ChannelMembers.Count(x => !channelIds.Contains(x.ChannelId) || !userIds.Contains(x.UserId)));
            None("頻道成員不重複", data.ChannelMembers.Count - memberships.Count);

            var directPairs = data.Channels
                .Where(x => x.Type == ChannelType.Direct)
                .Select(c => data.ChannelMembers
                    .Where(m => m.ChannelId == c.Id)
                    .Select(m => m.UserId)
                    .Order()
                    .ToList())
                .ToList();
            None("私訊恰有兩位成員", directPairs.Count(x => x.Count != 2));
            None("任兩人只有一個私訊",
                directPairs.Count - directPairs.Select(x => string.Join(",", x)).Distinct().Count());

            None("訊息指向存在的頻道，發送者為成員",
                data.Messages.Count(x => !channelIds.Contains(x.ChannelId)
                    || !userIds.Contains(x.SenderId)
                    || !memberships.Contains((x.ChannelId, x.SenderId))));
            None("@提及對象為頻道成員且出現在內容中",
                data.Messages.Count(m => m.MentionUserIds.Any(id =>
                {
                    var user = data.Users.FirstOrDefault(u => u.Id == id);
                    return user is null
                        || !memberships.Contains((m.ChannelId, id))
                        || !m.Content.Contains($"@{user.UserName}");
                })));
            None("訊息內容長度 1~1000 字",
                data.Messages.Count(x => x.Content.Trim().Length is < 1 or > 1000));
            None("已讀標記指向存在的成員關係",
                data.ReadMarkers.Count(x => !memberships.Contains((x.ChannelId, x.UserId))));
            None("已讀標記不重複",
                data.ReadMarkers.Count - data.ReadMarkers.Select(x => (x.ChannelId, x.UserId)).Distinct().Count());
            None("禁言指向存在的使用者", data.Mutes.Count(x => !userIds.Contains(x.UserId)));
            None("稽核紀錄指向存在的使用者", data.AuditLogs.Count(x => !userIds.Contains(x.ActorUserId)));
        }

        private static void CheckCounts(List<SeedCheck> checks, SeedDataSet data, DateTime utcNow)
        {
            void Count(string name, int actual) =>
                Add(checks, name, actual == ExpectedCount, $"{actual}/{ExpectedCount} 筆");

            Count("使用者：Admin", data.Users.Count(x => x.Role == UserRole.Admin));
            Count("使用者：Member", data.Users.Count(x => x.Role == UserRole.Member));

            // 封存頻道獨立計算，三種類型只算未封存的
            foreach (var type in Enum.GetValues<ChannelType>())
            {
                Count($"頻道：{type}", data.Channels.Count(x => x.Type == type && !x.IsArchived));
            }

            Count("頻道：已封存", data.Channels.Count(x => x.IsArchived));

            Count("禁言：進行中", data.Mutes.Count(x => x.Until > utcNow));
            Count("禁言：已到期", data.Mutes.Count(x => x.Until <= utcNow));

            // 訊息四類互斥：已刪除 > 含提及 > 依發送時間分可否編輯
            var alive = data.Messages.Where(x => !x.IsDeleted).ToList();
            var plain = alive.Where(x => x.MentionUserIds.Count == 0).ToList();
            Count("訊息：可編輯（未滿 5 分鐘）", plain.Count(x => utcNow - x.CreatedAt < EditWindow));
            Count("訊息：不可編輯（超過 5 分鐘）", plain.Count(x => utcNow - x.CreatedAt >= EditWindow));
            Count("訊息：已刪除", data.Messages.Count(x => x.IsDeleted));
            Count("訊息：含 @提及", alive.Count(x => x.MentionUserIds.Count > 0));

            // 晚於 LastReadAt 且非自己發送（R10）
            List<Message> Unread(ReadMarker marker) => data.Messages
                .Where(x => x.ChannelId == marker.ChannelId
                    && x.SenderId != marker.UserId
                    && x.CreatedAt > marker.LastReadAt)
                .ToList();

            Count("已讀標記：有未讀", data.ReadMarkers.Count(x => Unread(x).Any(m => !m.IsDeleted)));
            Count("已讀標記：全已讀", data.ReadMarkers.Count(x =>
                Unread(x).Count == 0 && data.Messages.Any(m => m.ChannelId == x.ChannelId)));
        }

        private static void CheckDates(List<SeedCheck> checks, SeedDataSet data, DateTime utcNow)
        {
            void Within(string name, IEnumerable<DateTime> values, bool allowFuture)
            {
                var broken = values.Count(x => x.Kind != DateTimeKind.Utc
                    || x < utcNow - DateRange
                    || x > (allowFuture ? utcNow + DateRange : utcNow));
                Add(checks, $"日期相對今天（UTC，前後 30 天內）：{name}", broken == 0, $"違反 {broken} 筆");
            }

            Within("訊息時間", data.Messages.Select(x => x.CreatedAt), allowFuture: false);
            Within("已讀時間", data.ReadMarkers.Select(x => x.LastReadAt), allowFuture: false);
            Within("禁言期限", data.Mutes.Select(x => x.Until), allowFuture: true);
            Within("稽核時間", data.AuditLogs.Select(x => x.CreatedAt), allowFuture: false);
        }

        private static void Add(List<SeedCheck> checks, string name, bool passed, string detail) =>
            checks.Add(new SeedCheck(name, passed, detail));
    }
}
