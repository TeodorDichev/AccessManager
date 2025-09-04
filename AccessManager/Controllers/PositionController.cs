using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Position;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class PositionController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly PositionService _positionService;

        public PositionController(UserService userService, LogService logService, PositionService positionService)
        {
            _logService = logService;
            _userService = userService;
            _positionService = positionService;
        }


        [HttpGet]
        public IActionResult SearchPositions(string term)
        {
            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var results = _positionService.GetPositions()
                .Where(u => string.IsNullOrEmpty(term) || u.Description.ToLowerInvariant().Contains(termLower))
                .Select(u => new { id = u.Id, text = u.Description })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public IActionResult PositionList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            PositionListViewModel model = new PositionListViewModel
            {
                Positions = _positionService.GetPositionsPaged(page),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePosition(string name)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.Full)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("PositionList");
            }

            if (_positionService.ExistsPositionWithDescription(name))
            {
                TempData["Error"] = ExceptionMessages.PositionWithNameExists;
                return RedirectToAction("PositionList");
            }

            Position position = _positionService.CreatePosition(name);
            _logService.AddLog(loggedUser, LogAction.Add, position);

            return RedirectToAction("PositionList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeletePosition(Guid id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            // Here the user must be SuperAdmin to delete a position because it is directly a hard delete despite name and behavior
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("PositionList");
            }

            Position? position = _positionService.GetPosition(id);
            if (position == null)
            {
                TempData["Error"] = ExceptionMessages.PositionNotFound;
                return RedirectToAction("PositionList");
            }

            TempData["Success"] = "Службата е изтрита успешно";
            _logService.AddLog(loggedUser, LogAction.Delete, position);
            _positionService.DeletePosition(position);
            return RedirectToAction("PositionList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePositionName([FromBody] UpdatePositionNameViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var position = _positionService.GetPosition(model.Id);
            if (position == null)
            {
                return Json(new { success = false, message = ExceptionMessages.PositionNotFound });
            }

            _positionService.UpdatePositionDescription(position, model.Name);
            _logService.AddLog(loggedUser, LogAction.Edit, position);

            return Json(new { success = true });
        }
    }
}
