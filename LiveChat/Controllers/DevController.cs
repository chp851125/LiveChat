using LiveChat.DevTools;
using LiveChat.Models;
using LiveChat.Repositories;
using LiveChat.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveChat.Controllers
{
    /// <summary>Development 工具列的端點。非 Development 環境不會註冊。</summary>
    [DevelopmentOnly]
    [AllowAnonymous]
    public class DevController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SwitchUser(
            Guid userId,
            string? returnUrl,
            [FromServices] IRepository<User> users,
            [FromServices] IAuthService authService)
        {
            var user = users.GetById(userId);
            if (user is null)
            {
                TempData["DevError"] = "找不到使用者，請先重置測試資料。";
                return RedirectBack(returnUrl);
            }

            await HttpContext.SignInAsync(authService.CreatePrincipal(user));
            return RedirectBack(returnUrl);
        }

        [HttpPost]
        public IActionResult Reset(string? returnUrl, [FromServices] SeedResetService seedResetService)
        {
            var checks = seedResetService.Reset();
            var failed = checks.Where(x => !x.Passed).ToList();

            if (failed.Count == 0)
            {
                TempData["DevMessage"] = $"測試資料已重置，驗證 {checks.Count} 項全部通過。";
            }
            else
            {
                TempData["DevError"] = "驗證未通過，App_Data 未變更："
                    + string.Join("；", failed.Select(x => $"{x.Name}（{x.Detail}）"));
            }

            return RedirectBack(returnUrl);
        }

        // 登入頁不當作返回目標，切換使用者後改回首頁
        private IActionResult RedirectBack(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) && !returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Home");
    }
}
