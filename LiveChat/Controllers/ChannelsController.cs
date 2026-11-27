using LiveChat.Models;
using LiveChat.Services;
using LiveChat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LiveChat.Controllers
{
    public class ChannelsController : Controller
    {
        private readonly IChannelService _channelService;

        public ChannelsController(IChannelService channelService)
        {
            _channelService = channelService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = User.GetUserId();
            return View(new ChannelListViewModel
            {
                Joined = _channelService.GetJoinedChannels(userId),
                Joinable = _channelService.GetJoinableChannels(userId),
                Directs = _channelService.GetDirectConversations(userId),
                DirectCandidates = _channelService.GetDirectCandidates(userId)
            });
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateChannelViewModel());

        [HttpPost]
        public IActionResult Create(CreateChannelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var channel = _channelService.Create(model.Name, model.Type, User.GetUserId());
                TempData["StatusMessage"] = $"已建立頻道「{channel.Name}」。";
                return RedirectToAction(nameof(Details), new { id = channel.Id });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Details(Guid id) => ShowChannel(id);

        [HttpGet]
        public IActionResult Members(Guid id) => ShowChannel(id);

        [HttpPost]
        public IActionResult Join(Guid id) =>
            Execute(() => _channelService.Join(id, User.GetUserId()), "已加入頻道。",
                RedirectToAction(nameof(Details), new { id }));

        [HttpPost]
        public IActionResult Leave(Guid id) =>
            Execute(() => _channelService.Leave(id, User.GetUserId()), "已離開頻道。",
                RedirectToAction(nameof(Index)));

        [HttpPost]
        public IActionResult Invite(Guid id, Guid userId) =>
            Execute(() => _channelService.Invite(id, User.GetUserId(), userId), "已邀請成員。",
                RedirectToAction(nameof(Members), new { id }));

        [HttpPost]
        public IActionResult Remove(Guid id, Guid userId) =>
            Execute(() => _channelService.Remove(id, User.GetUserId(), userId), "已移除成員。",
                RedirectToAction(nameof(Members), new { id }));

        [HttpPost]
        public IActionResult Direct(Guid userId)
        {
            try
            {
                var channel = _channelService.GetOrCreateDirect(User.GetUserId(), userId);
                return RedirectToAction(nameof(Details), new { id = channel.Id });
            }
            catch (BusinessRuleException ex)
            {
                return RedirectToList(ex.Message);
            }
        }

        // Details 與 Members 共用同一份資料與權限檢查，View 依 action 名稱選取
        private IActionResult ShowChannel(Guid id)
        {
            var userId = User.GetUserId();
            try
            {
                var channel = _channelService.GetAccessibleChannel(id, userId);
                var members = _channelService.GetMembers(id);
                var canManage = channel.Type == ChannelType.Private
                    && channel.CreatedByUserId == userId
                    && !channel.IsArchived;

                return View(new ChannelViewModel
                {
                    Channel = channel,
                    Title = channel.Type == ChannelType.Direct
                        ? members.FirstOrDefault(x => x.Id != userId)?.DisplayName ?? channel.Name
                        : channel.Name,
                    Members = members,
                    IsMember = members.Any(x => x.Id == userId),
                    CanManageMembers = canManage,
                    InviteCandidates = canManage
                        ? _channelService.GetNonMembers(id)
                        : Array.Empty<User>()
                });
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
        }

        // 沒有權限時一律導回清單；其他規則錯誤回到原頁面提示
        private IActionResult Execute(Action action, string successMessage, IActionResult next)
        {
            try
            {
                action();
                TempData["StatusMessage"] = successMessage;
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return next;
        }

        private IActionResult RedirectToList(string errorMessage)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Index));
        }
    }
}
