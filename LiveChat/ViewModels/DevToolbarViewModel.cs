using LiveChat.Models;

namespace LiveChat.ViewModels
{
    public class DevToolbarViewModel
    {
        public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();

        public Guid? CurrentUserId { get; init; }

        public string ReturnUrl { get; init; } = "/";
    }
}
