using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class LogController : BaseController
    {
        [HttpGet]
        public IActionResult LogList()
        {
            return View();
        }
    }
}
