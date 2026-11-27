using LiveChat.Models;

namespace LiveChat.DevTools
{
    /// <summary>產生測試資料。Id 固定（重置後登入狀態仍有效），時間一律相對傳入的現在時間。</summary>
    public class SeedGenerator
    {
        // Id 第四段用來區分實體種類
        private const int UserKind = 1;
        private const int ChannelKind = 2;
        private const int ChannelMemberKind = 3;
        private const int MessageKind = 4;
        private const int ReadMarkerKind = 5;
        private const int MuteKind = 6;

        public SeedDataSet Generate(DateTime utcNow)
        {
            var data = new SeedDataSet();

            DateTime Ago(int days = 0, int hours = 0, int minutes = 0) =>
                utcNow - new TimeSpan(days, hours, minutes, 0);

            User AddUser(string userName, string displayName, UserRole role)
            {
                var user = new User
                {
                    Id = SeedId(UserKind, data.Users.Count + 1),
                    UserName = userName,
                    DisplayName = displayName,
                    Role = role
                };
                data.Users.Add(user);
                return user;
            }

            // 第一位成員即建立者
            Channel AddChannel(string name, ChannelType type, bool isArchived, params User[] members)
            {
                var channel = new Channel
                {
                    Id = SeedId(ChannelKind, data.Channels.Count + 1),
                    Name = name,
                    Type = type,
                    IsArchived = isArchived,
                    CreatedByUserId = members[0].Id
                };
                data.Channels.Add(channel);

                foreach (var member in members)
                {
                    data.ChannelMembers.Add(new ChannelMember
                    {
                        Id = SeedId(ChannelMemberKind, data.ChannelMembers.Count + 1),
                        ChannelId = channel.Id,
                        UserId = member.Id
                    });
                }

                return channel;
            }

            void AddMessage(Channel channel, User sender, DateTime createdAt, string content,
                bool isDeleted = false, User[]? mentions = null) =>
                data.Messages.Add(new Message
                {
                    Id = SeedId(MessageKind, data.Messages.Count + 1),
                    ChannelId = channel.Id,
                    SenderId = sender.Id,
                    Content = content,
                    CreatedAt = createdAt,
                    IsDeleted = isDeleted,
                    MentionUserIds = mentions?.Select(x => x.Id).ToList() ?? new List<Guid>()
                });

            void AddReadMarker(User user, Channel channel, DateTime lastReadAt) =>
                data.ReadMarkers.Add(new ReadMarker
                {
                    Id = SeedId(ReadMarkerKind, data.ReadMarkers.Count + 1),
                    UserId = user.Id,
                    ChannelId = channel.Id,
                    LastReadAt = lastReadAt
                });

            void AddMute(User user, DateTime until) =>
                data.Mutes.Add(new Mute
                {
                    Id = SeedId(MuteKind, data.Mutes.Count + 1),
                    UserId = user.Id,
                    Until = until
                });

            // 使用者
            var admin1 = AddUser("admin1", "陳怡君", UserRole.Admin);
            var admin2 = AddUser("admin2", "林志明", UserRole.Admin);
            var admin3 = AddUser("admin3", "黃淑芬", UserRole.Admin);
            var member1 = AddUser("member1", "王小明", UserRole.Member);
            var member2 = AddUser("member2", "李美玲", UserRole.Member);
            var member3 = AddUser("member3", "張家豪", UserRole.Member);

            // 公開頻道
            var announce = AddChannel("公告", ChannelType.Public, false,
                admin1, admin2, admin3, member1, member2, member3);
            var lounge = AddChannel("閒聊", ChannelType.Public, false,
                member1, member2, member3, admin2);
            var tech = AddChannel("技術討論", ChannelType.Public, false,
                member2, member1, admin3);

            // 私人頻道
            AddChannel("管理團隊", ChannelType.Private, false, admin1, admin2, admin3);
            var project = AddChannel("專案A", ChannelType.Private, false, member1, member2, admin1);
            AddChannel("讀書會", ChannelType.Private, false, member2, member3);

            // 私訊
            var directA = AddChannel("admin1, member1", ChannelType.Direct, false, admin1, member1);
            var directB = AddChannel("member1, member2", ChannelType.Direct, false, member1, member2);
            AddChannel("member2, member3", ChannelType.Direct, false, member2, member3);

            // 已封存
            var oldProject = AddChannel("舊專案", ChannelType.Public, true, admin1, member1, member2);
            AddChannel("活動籌備", ChannelType.Public, true, member3, member1);
            AddChannel("臨時小組", ChannelType.Private, true, admin2, member1);

            // 訊息：可編輯（發送未滿 5 分鐘）
            AddMessage(lounge, member1, Ago(minutes: 1), "中午有人要一起訂便當嗎？");
            AddMessage(tech, member2, Ago(minutes: 2), "我剛更新了開發環境的說明文件。");
            AddMessage(directA, admin1, Ago(minutes: 3), "下午有空討論一下排程嗎？");

            // 訊息：不可編輯（超過 5 分鐘）
            AddMessage(announce, admin1, Ago(days: 3), "本週五下午進行系統維護，請提早存檔。");
            AddMessage(project, member2, Ago(days: 1), "第一版畫面草稿已放到共用資料夾。");
            AddMessage(oldProject, admin1, Ago(days: 20), "本專案已結案，感謝大家。");

            // 訊息：已刪除
            AddMessage(lounge, member2, Ago(hours: 2), "貼錯頻道了。", isDeleted: true);
            AddMessage(tech, admin3, Ago(hours: 5), "這段設定有誤，請忽略。", isDeleted: true);
            AddMessage(directB, member2, Ago(days: 1, hours: 1), "晚點再跟你說。", isDeleted: true);

            // 訊息：含 @提及
            AddMessage(announce, admin1, Ago(days: 2), "@member1 @member2 請協助確認維護時段。",
                mentions: new[] { member1, member2 });
            AddMessage(project, member1, Ago(hours: 6), "@admin1 需求清單整理好了，請過目。",
                mentions: new[] { admin1 });
            AddMessage(lounge, member3, Ago(minutes: 30), "@admin2 下午茶要喝什麼？",
                mentions: new[] { admin2 });

            // 已讀標記：有未讀
            AddReadMarker(member2, announce, Ago(days: 2, hours: 12));
            AddReadMarker(member1, directA, Ago(days: 1));
            AddReadMarker(admin1, project, Ago(hours: 12));

            // 已讀標記：全已讀
            AddReadMarker(admin1, announce, Ago(days: 1));
            AddReadMarker(member1, lounge, Ago(minutes: 10));
            AddReadMarker(member2, tech, Ago(hours: 1));

            // 禁言：進行中
            AddMute(member3, utcNow.AddHours(1));
            AddMute(admin2, utcNow.AddDays(1));
            AddMute(admin3, utcNow.AddDays(7));

            // 禁言：已到期
            AddMute(member1, Ago(hours: 1));
            AddMute(member2, Ago(days: 1));
            AddMute(admin1, Ago(days: 7));

            return data;
        }

        private static Guid SeedId(int kind, int number) =>
            Guid.Parse($"00000000-0000-0000-{kind:D4}-{number:D12}");
    }
}
