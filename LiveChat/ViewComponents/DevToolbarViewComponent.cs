using System.Security.Claims;
using LiveChat.Models;
using LiveChat.Repositories;
using LiveChat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LiveChat.ViewComponents
{
    public class DevToolbarViewComponent : ViewComponent
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IRepository<User> _users;

        public DevToolbarViewComponent(IWebHostEnvironment environment, IRepository<User> users)
        {
            _environment = environment;
            _users = users;
        }

        public IViewComponentResult Invoke()
        {
            if (!_environment.IsDevelopment())
            {
                return Content(string.Empty);
            }

            var idValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(new DevToolbarViewModel
            {
                Users = _users.GetAll(),
                CurrentUserId = Guid.TryParse(idValue, out var id) ? id : null,
                ReturnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}"
            });
        }
    }
}
