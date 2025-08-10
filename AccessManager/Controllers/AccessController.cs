using AccessManager.Data;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DepartmentUnitService _departmentUnitService;

        public AccessController(Context context, UserService userService,
            AccessService accessService, DepartmentUnitService departmentUnitService)
        {
            _userService = userService;
            _accessService = accessService;
            _departmentUnitService = departmentUnitService;
        }

        [HttpGet]
        public IActionResult AccessList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            List<AccessListItemViewModel> allAccesses = _accessService.GetAccesses();
            int totalUsers = allAccesses.Count();

            List<AccessListItemViewModel> accesses = allAccesses.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var model = new AccessListViewModel
            {
                Accesses = accesses,
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteAccess(string id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessToDelete = _accessService.GetAccess(id);
            if (accessToDelete == null) return NotFound();

            _accessService.SoftDeleteAccess(accessToDelete);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HardDeleteAccesses()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (loggedUser.WritingAccess == AuthorityType.SuperAdmin) _accessService.HardDeleteAccesses();
            return RedirectToAction("AccessList");
        }

        [HttpPost]
        public IActionResult UpdateDirective([FromBody] UpdateUserAccessDirectiveViewModel model)
        {
            // Validate model
            if (model == null || model.AccessId == Guid.Empty || string.IsNullOrEmpty(model.DirectiveId))
                return Json(new { success = false, message = "Невалидни данни" });

            var access = _accessService.GetUserAccess(model.AccessId.ToString(), model.Username);
            if (access == null)
                return Json(new { success = false, message = "Достъпът не е намерен" });

            _accessService.UpdateAccessDirective(access, model.DirectiveId);

            return Json(new { success = true, message = "Заповедта е обновена успешно" });
        }
        [HttpGet]
        public IActionResult AccessUsersList(int page = 1)
        {
            return View();
        }
    }
}
