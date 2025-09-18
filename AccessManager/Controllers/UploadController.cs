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
        public IActionResult UploadCompleteTable(IFormFile file, bool drop)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("Index", "Home");
            }
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = ExceptionMessages.FileNotUploaded;
                return View("Upload");
            }

            try
            {
                _fileService.UploadCompleteTable(file, drop);
            }
            catch (Exception e)
            {
                TempData["Error"] = e.Message;
                return View("Upload");
            }

            TempData["Success"] = "Промените са успешно записани";
            return drop ? RedirectToAction("Index", "Home") : View("Upload");
        }

        [HttpPost]
        public IActionResult DeleteDb()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _fileService.DeleteDb();
            }
            catch (Exception e)
            {
                TempData["Error"] = e.Message;
                return View("Upload");
            }

            TempData["Success"] = "Промените са успешно записани";
            return RedirectToAction("Index", "Home");
        }
    }
}
