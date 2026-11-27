using LiveChat.Models;
using LiveChat.Repositories;

namespace LiveChat.DevTools
{
    /// <summary>一整套測試資料。</summary>
    public class SeedDataSet
    {
        public List<User> Users { get; init; } = new();

        public List<Channel> Channels { get; init; } = new();

        public List<ChannelMember> ChannelMembers { get; init; } = new();

        public List<Message> Messages { get; init; } = new();

        public List<ReadMarker> ReadMarkers { get; init; } = new();

        public List<Mute> Mutes { get; init; } = new();

        public List<AuditLog> AuditLogs { get; init; } = new();

        public void Save(string folderPath)
        {
            Write(folderPath, Users);
            Write(folderPath, Channels);
            Write(folderPath, ChannelMembers);
            Write(folderPath, Messages);
            Write(folderPath, ReadMarkers);
            Write(folderPath, Mutes);
            Write(folderPath, AuditLogs);
        }

        public static SeedDataSet Load(string folderPath) => new()
        {
            Users = Read<User>(folderPath),
            Channels = Read<Channel>(folderPath),
            ChannelMembers = Read<ChannelMember>(folderPath),
            Messages = Read<Message>(folderPath),
            ReadMarkers = Read<ReadMarker>(folderPath),
            Mutes = Read<Mute>(folderPath),
            AuditLogs = Read<AuditLog>(folderPath)
        };

        private static void Write<T>(string folderPath, List<T> items) =>
            JsonDataFile.Write(Path.Combine(folderPath, JsonDataFile.FileName<T>()), items);

        private static List<T> Read<T>(string folderPath) =>
            JsonDataFile.Read<T>(Path.Combine(folderPath, JsonDataFile.FileName<T>()));
    }
}
