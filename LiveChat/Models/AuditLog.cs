namespace LiveChat.Models
{
    public class AuditLog : IEntity
    {
        public Guid Id { get; set; }

        public Guid ActorUserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string TargetDescription { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
