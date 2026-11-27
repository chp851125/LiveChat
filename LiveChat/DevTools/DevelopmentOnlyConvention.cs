using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace LiveChat.DevTools
{
    /// <summary>移除標記 DevelopmentOnly 的 Controller，使其路由不會建立。非 Development 環境才註冊。</summary>
    public class DevelopmentOnlyConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            var targets = application.Controllers
                .Where(x => x.Attributes.OfType<DevelopmentOnlyAttribute>().Any())
                .ToList();

            foreach (var controller in targets)
            {
                application.Controllers.Remove(controller);
            }
        }
    }
}
