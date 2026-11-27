using System.Security.Claims;
using LiveChat.Models;

namespace LiveChat.Services
{
    public interface IAuthService
    {
        /// <summary>驗證 Mock 帳號密碼；失敗丟 BusinessRuleException。</summary>
        User Authenticate(string userName, string password);

        ClaimsPrincipal CreatePrincipal(User user);
    }
}
