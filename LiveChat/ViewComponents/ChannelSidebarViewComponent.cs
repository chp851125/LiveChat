using LiveChat.Controllers;
using LiveChat.Services;
using LiveChat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LiveChat.ViewComponents
{
    public class ChannelSidebarViewComponent : ViewComponent
    {
        private readonly IChannelService _channelService;
        private readonly IReadMarkerService _readMarkerService;

        public ChannelSidebarViewComponent(IChannelService channelService, IReadMarkerService readMarkerService)
        {
            _channelService = channelService;
            _readMarkerService = readMarkerService;
        }

        public IViewComponentResult Invoke(Guid currentChannelId)
        {
            var userId = UserClaimsPrincipal.GetUserId();
            return View(new ChannelSidebarViewModel
            {
                Joined = _channelService.GetJoinedChannels(userId),
                Directs = _channelService.GetDirectConversations(userId),
                UnreadCounts = _readMarkerService.GetUnreadCounts(userId),
                CurrentChannelId = currentChannelId
            });
        }
    }
}
