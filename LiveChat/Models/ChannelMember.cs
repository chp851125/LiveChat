namespace LiveChat.Models
{
    public class ChannelMember : IEntity
    {
        public Guid Id { get; set; }

        public Guid ChannelId { get; set; }

        public Guid UserId { get; set; }
    }
}
