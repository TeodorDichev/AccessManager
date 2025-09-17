using AccessManager.Services;
using AccessManager.Utills;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AccessManager.Controllers
{
    public class UploadController : BaseController
    {
        private readonly UserService _userService;
        private readonly UnitService _unitService;
        private readonly FileService _fileService;

        public UploadController(UserService userService, UnitService unitService, FileService fileService)
        {
            _userService = userService;
            _unitService = unitService;
            _fileService = fileService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("Index", "Home");
            }

            return View("Upload");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadUsers(IFormFile file, string targetTable, bool dropTable)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = ExceptionMessages.FileNotUploaded;
                return View("Upload");
            }

            TempData["Success"] = "Промените са успешно записани";
            return View("Upload");
        }
    }

}
