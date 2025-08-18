using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Log;
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

            var logs = _logService.GetLogs(page);

            var model = new LogListViewModel
            {
                Logs = logs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)_logService.GetLogsCount() / Constants.ItemsPerPage)
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteAllLogs()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if(loggedUser.WritingAccess != Data.Enums.AuthorityType.SuperAdmin)
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
