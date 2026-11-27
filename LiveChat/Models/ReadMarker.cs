namespace LiveChat.Models
{
    public class ReadMarker : IEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid ChannelId { get; set; }

        public DateTime LastReadAt { get; set; }
    }
}
