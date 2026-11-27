using LiveChat.Models;
using LiveChat.Services;

namespace LiveChat.ViewModels
{
    public class ChannelListViewModel
    {
        public IReadOnlyList<Channel> Joined { get; init; } = Array.Empty<Channel>();

        public IReadOnlyList<Channel> Joinable { get; init; } = Array.Empty<Channel>();

        public IReadOnlyList<DirectConversation> Directs { get; init; } = Array.Empty<DirectConversation>();

        public IReadOnlyList<User> DirectCandidates { get; init; } = Array.Empty<User>();
    }
}
