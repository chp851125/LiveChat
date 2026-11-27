namespace LiveChat.Models
{
    public class Mute : IEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTime Until { get; set; }
    }
}
