using AccessManager.Data;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;

        public AccessController(Context context, UserService userService,
            AccessService accessService, DirectiveService directiveService)
        {
            _userService = userService;
            _accessService = accessService;
            _directiveService = directiveService;
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

        [HttpGet]
        public IActionResult AccessUsersList(int page = 1)
        {
            return View();
        }
    }
}
