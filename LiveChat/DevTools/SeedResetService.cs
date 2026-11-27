using LiveChat.Models;
using LiveChat.Repositories;

namespace LiveChat.DevTools
{
    /// <summary>重新產生 SeedData，驗證通過後覆蓋 App_Data。</summary>
    public class SeedResetService
    {
        private readonly object _resetLock = new();
        private readonly SeedGenerator _generator = new();
        private readonly SeedValidator _validator = new();

        private readonly string _seedPath;
        private readonly IRepository<User> _users;
        private readonly IRepository<Channel> _channels;
        private readonly IRepository<ChannelMember> _channelMembers;
        private readonly IRepository<Message> _messages;
        private readonly IRepository<ReadMarker> _readMarkers;
        private readonly IRepository<Mute> _mutes;
        private readonly IRepository<AuditLog> _auditLogs;

        public SeedResetService(
            IWebHostEnvironment environment,
            IRepository<User> users,
            IRepository<Channel> channels,
            IRepository<ChannelMember> channelMembers,
            IRepository<Message> messages,
            IRepository<ReadMarker> readMarkers,
            IRepository<Mute> mutes,
            IRepository<AuditLog> auditLogs)
        {
            _seedPath = Path.Combine(environment.ContentRootPath, SeedDataInitializer.SeedFolder);
            _users = users;
            _channels = channels;
            _channelMembers = channelMembers;
            _messages = messages;
            _readMarkers = readMarkers;
            _mutes = mutes;
            _auditLogs = auditLogs;
        }

        /// <summary>回傳驗證結果；任一項未通過時 App_Data 維持原狀。</summary>
        public IReadOnlyList<SeedCheck> Reset()
        {
            lock (_resetLock)
            {
                var utcNow = DateTime.UtcNow;
                _generator.Generate(utcNow).Save(_seedPath);

                // 從寫出的檔案讀回再驗證，確保檢查到的是實際內容
                var data = SeedDataSet.Load(_seedPath);
                var checks = _validator.Validate(data, utcNow);
                if (checks.All(x => x.Passed))
                {
                    _users.ReplaceAll(data.Users);
                    _channels.ReplaceAll(data.Channels);
                    _channelMembers.ReplaceAll(data.ChannelMembers);
                    _messages.ReplaceAll(data.Messages);
                    _readMarkers.ReplaceAll(data.ReadMarkers);
                    _mutes.ReplaceAll(data.Mutes);
                    _auditLogs.ReplaceAll(data.AuditLogs);
                }

                return checks;
            }
        }
    }
}
