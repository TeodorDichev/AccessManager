using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class LogController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;

        public LogController(UserService userService, LogService logService)
        {
            _userService = userService;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult LogList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin;

            var result = _logService.GetLogsPaged(page);

            return View(result);
        }

        [HttpPost]
        public IActionResult DeleteAllLogs()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (loggedUser.WritingAccess != Data.Enums.AuthorityType.SuperAdmin)
            {
                TempData["Error"] = "Нямате достъп";
            }
            else
            {
                TempData["Success"] = "Успешно изтрихте всички записи";
                _logService.DeleteAllLogs();
            }
            return RedirectToAction("LogList");
        }
    }
}
