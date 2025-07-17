using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccessManager.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = context.HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                ViewData["Username"] = username;
            }

            base.OnActionExecuting(context);
        }
    }
}
