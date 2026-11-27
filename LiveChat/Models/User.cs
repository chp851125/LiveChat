namespace LiveChat.Models
{
    public class User : IEntity
    {
        public Guid Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Member;
    }
}
