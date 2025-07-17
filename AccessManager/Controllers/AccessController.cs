using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        [HttpGet]
        public IActionResult AccessList()
        {
            return View();
        }
    }
}
