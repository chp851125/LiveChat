namespace LiveChat.Models
{
    public class Message : IEntity
    {
        public Guid Id { get; set; }

        public Guid ChannelId { get; set; }

        public Guid SenderId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; }

        public List<Guid> MentionUserIds { get; set; } = new();
    }
}
