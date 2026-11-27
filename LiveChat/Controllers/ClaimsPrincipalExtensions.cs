using System.Security.Claims;

namespace LiveChat.Controllers
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>目前登入者的 Id（NameIdentifier claim）。</summary>
        public static Guid GetUserId(this ClaimsPrincipal principal) =>
            Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
