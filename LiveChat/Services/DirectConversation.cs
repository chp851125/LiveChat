using LiveChat.Models;

namespace LiveChat.Services
{
    /// <summary>某人眼中的一則私訊：頻道與對方。</summary>
    public record DirectConversation(Channel Channel, User OtherUser);
}
