using System.Security.Claims;
using LiveChat.Models;
using LiveChat.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LiveChat.Services
{
    public class AuthService : IAuthService
    {
        // 所有 Mock 帳號共用的密碼（README 有記載）
        private const string MockPassword = "Passw0rd!";

        private readonly IRepository<User> _users;

        public AuthService(IRepository<User> users)
        {
            _users = users;
        }

        public User Authenticate(string userName, string password)
        {
            var name = userName.Trim();
            var user = _users
                .Find(x => string.Equals(x.UserName, name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (user is null || password != MockPassword)
            {
                throw new BusinessRuleException("帳號或密碼錯誤。");
            }

            return user;
        }

        public ClaimsPrincipal CreatePrincipal(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
