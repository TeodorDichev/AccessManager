using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Directive;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class DirectiveController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;
        private readonly DepartmentService _departmentUnitService;

        public DirectiveController(Context context, UserService userService,
            AccessService accessService, DepartmentService departmentUnitService, DirectiveService directiveService)
        {
            _userService = userService;
            _accessService = accessService;
            _departmentUnitService = departmentUnitService;
            _directiveService = directiveService;
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

            _directiveService.CreateDirective(name);

            return RedirectToAction("DirectiveList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDirective(Guid id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Directive? directive = _directiveService.GetDirective(id);
            if(directive != null)
            {
                _directiveService.DeleteDirective(directive);
            }

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

            var access = _accessService.GetUserAccess(model.AccessId.ToString(), model.Username);
            if (access == null)
                return Json(new { success = false, message = "Достъпът не е намерен" });

            _directiveService.UpdateAccessDirective(access, model.DirectiveId);

            return Json(new { success = true, message = "Заповедта е обновена успешно" });
        }

    }
}
