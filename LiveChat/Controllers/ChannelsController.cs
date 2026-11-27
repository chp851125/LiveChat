using LiveChat.Models;
using LiveChat.Services;
using LiveChat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LiveChat.Controllers
{
    public class ChannelsController : Controller
    {
        private readonly IChannelService _channelService;
        private readonly IMessageService _messageService;
        private readonly IReadMarkerService _readMarkerService;

        public ChannelsController(
            IChannelService channelService,
            IMessageService messageService,
            IReadMarkerService readMarkerService)
        {
            _channelService = channelService;
            _messageService = messageService;
            _readMarkerService = readMarkerService;
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
                DirectCandidates = _channelService.GetDirectCandidates(userId),
                UnreadCounts = _readMarkerService.GetUnreadCounts(userId)
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
        public IActionResult Details(Guid id, Guid? before) => ShowChatRoom(id, before);

        [HttpGet]
        public IActionResult Members(Guid id) => ShowChannel(id);

        [HttpPost]
        public IActionResult Send(Guid id, string? content)
        {
            try
            {
                _messageService.Send(id, User.GetUserId(), content ?? string.Empty);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                // 直接重新顯示聊天室，保留輸入內容
                TempData["ErrorMessage"] = ex.Message;
                return ShowChatRoom(id, before: null, draft: content);
            }
        }

        [HttpPost]
        public IActionResult EditMessage(Guid id, Guid messageId, string? content, Guid? before)
        {
            try
            {
                _messageService.Edit(messageId, User.GetUserId(), content ?? string.Empty);
                return RedirectToAction(nameof(Details), new { id, before });
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return ShowChatRoom(id, before, draft: content, editingMessageId: messageId);
            }
        }

        [HttpPost]
        public IActionResult DeleteMessage(Guid id, Guid messageId, Guid? before) =>
            Execute(() => _messageService.Delete(messageId, User.GetUserId()), "已刪除訊息。",
                RedirectToAction(nameof(Details), new { id, before }));

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

        // 進入聊天室即更新已讀（R10）；POST 失敗時也由此重新顯示，所以指定 View 名稱
        private IActionResult ShowChatRoom(
            Guid id, Guid? before, string? draft = null, Guid? editingMessageId = null)
        {
            var userId = User.GetUserId();
            try
            {
                var model = BuildChannelViewModel(id, userId);
                if (!model.IsMember)
                {
                    return View(nameof(Details), model);
                }

                // 先標記再讀取：期間新進的訊息仍算未讀，不會漏掉
                _readMarkerService.MarkAsRead(id, userId);

                return View(nameof(Details), model with
                {
                    Messages = _messageService.GetMessages(id, userId, before),
                    SendRestriction = _messageService.GetSendRestriction(id, userId),
                    Before = before,
                    Draft = draft,
                    EditingMessageId = editingMessageId
                });
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
        }

        private IActionResult ShowChannel(Guid id)
        {
            try
            {
                return View(BuildChannelViewModel(id, User.GetUserId()));
            }
            catch (ChannelAccessDeniedException ex)
            {
                return RedirectToList(ex.Message);
            }
        }

        // 聊天室與成員頁共用同一份資料與權限檢查
        private ChannelViewModel BuildChannelViewModel(Guid id, Guid userId)
        {
            var channel = _channelService.GetAccessibleChannel(id, userId);
            var members = _channelService.GetMembers(id);
            var canManage = channel.Type == ChannelType.Private
                && channel.CreatedByUserId == userId
                && !channel.IsArchived;

            return new ChannelViewModel
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
            };
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
