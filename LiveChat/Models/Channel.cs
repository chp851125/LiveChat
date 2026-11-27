namespace LiveChat.Models
{
    public class Channel : IEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ChannelType Type { get; set; }

        public bool IsArchived { get; set; }

        public Guid CreatedByUserId { get; set; }
    }
}
