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
        private readonly UserAccessService _userAccessService;
        private readonly DirectiveService _directiveService;
        private readonly DepartmentService _departmentUnitService;

        public DirectiveController(Context context, UserService userService, LogService logService,
            AccessService accessService, DepartmentService departmentUnitService, DirectiveService directiveService, UserAccessService userAccessService)
        {
            _logService = logService;
            _userService = userService;
            _accessService = accessService;
            _departmentUnitService = departmentUnitService;
            _directiveService = directiveService;
            _userAccessService = userAccessService;
        }

        [HttpGet]
        public IActionResult SearchDirectives(string term = "")
        {
            var all = _directiveService.GetDirectives().Select(a => new { a.Id, a.Name }).ToList();
            var qLower = (term ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => string.IsNullOrEmpty(qLower) || a.Name.ToLowerInvariant().Contains(qLower))
                .OrderBy(a => a.Name)
                .Take(10)
                .Select(a => new { id = a.Id, text = a.Name })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult DirectiveList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            DirectiveListViewModel model = new DirectiveListViewModel
            {
                Directives = _directiveService.GetDirectivesPaged(page),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDirective(string name)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.Full)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("DirectiveList");
            }

            if (_directiveService.ExistsDirectiveWithName(name))
            {
                TempData["Error"] = ExceptionMessages.DirectiveWithNameExists;
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

            // Here the user must be SuperAdmin to delete a directive because it is directly a hard delete despite name and behavior
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("DirectiveList");
            }

            Directive? directive = _directiveService.GetDirective(id);
            if (directive == null)
            {
                TempData["Error"] = ExceptionMessages.DirectiveNotFound;
                return RedirectToAction("DirectiveList");
            }
            else if (!_directiveService.CanDeleteDirective(directive))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeDeletedDueToDependencies;
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
                return Json(new { success = false, message = ExceptionMessages.DirectiveNotFound });
            }

            _directiveService.UpdateDirectiveName(directive, model.Name);
            _logService.AddLog(loggedUser, LogAction.Edit, directive);

            return Json(new { success = true });
        }
    }
}
