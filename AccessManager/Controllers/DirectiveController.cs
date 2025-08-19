using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Directive;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class DirectiveController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;
        private readonly DepartmentService _departmentUnitService;

        public DirectiveController(Context context, UserService userService, LogService logService,
            AccessService accessService, DepartmentService departmentUnitService, DirectiveService directiveService)
        {
            _logService = logService;
            _userService = userService;
            _accessService = accessService;
            _departmentUnitService = departmentUnitService;
            _directiveService = directiveService;
        }

        [HttpGet]
        public IActionResult GetDirectives(string q="")
        {
            var all = _directiveService.GetDirectives().Select(a => new { a.Id, a.Name }).ToList();
            var qLower = (q ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => string.IsNullOrEmpty(qLower) || a.Name.ToLowerInvariant().Contains(qLower))
                .OrderBy(a => a.Name)
                .Take(30)
                .Select(a => new { id = a.Id, text = a.Name })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        public IActionResult DirectiveList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var totalDirectives = _directiveService.GetDirectives().Count();

            var directives = _directiveService.GetDirectives()
                .OrderBy(d => d.Name)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

            var model = new DirectiveListViewModel
            {
                Directives = directives,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalDirectives / (double)Constants.ItemsPerPage)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDirective(string name)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if(_directiveService.ExistsDirectiveWithName(name))
            {
                TempData["Error"] = "Вече съществува заповед с това име";
                return RedirectToAction("DirectiveList");
            }

            Directive dir = _directiveService.CreateDirective(name);
            _logService.AddLog(loggedUser, LogAction.Add, dir);

            return RedirectToAction("DirectiveList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteDirective(Guid id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Directive? directive = _directiveService.GetDirective(id);
            if(directive == null)
            {            
                TempData["Error"] = "Не съществува такава заповед";
                return RedirectToAction("DirectiveList");
            }
            else if(!_directiveService.CanDeleteDirective(directive))
            {
                TempData["Error"] = "Не може да изтриете тази заповед";
                return RedirectToAction("DirectiveList");
            }

            TempData["Success"] = "Заповедта е изтрита успешно";
            _logService.AddLog(loggedUser, LogAction.Delete, directive);
            _directiveService.SoftDeleteDirective(directive);
            return RedirectToAction("DirectiveList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDirectiveName([FromBody] UpdateDirectiveNameViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var directive = _directiveService.GetDirective(model.Id);
            if (directive == null)
            {
                return Json(new { success = false, message = "Заповедта не е намерена" });
            }

            _directiveService.UpdateDirectiveName(directive, model.Name);
            _logService.AddLog(loggedUser, LogAction.Edit, directive);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDirective([FromBody] UpdateUserAccessDirectiveViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (model == null || model.AccessId == Guid.Empty || model.DirectiveId == Guid.Empty)
                return Json(new { success = false, message = "Невалидни данни" });

            var userAccess = _accessService.GetUserAccess(model.AccessId.ToString(), model.Username);
            if (userAccess == null)
                return Json(new { success = false, message = "Достъпът не е намерен" });

            _directiveService.UpdateAccessDirective(userAccess, model.DirectiveId);
            _logService.AddLog(loggedUser, LogAction.Edit, userAccess);

            return Json(new { success = true, message = "Заповедта е обновена успешно" });
        }
    }
}
