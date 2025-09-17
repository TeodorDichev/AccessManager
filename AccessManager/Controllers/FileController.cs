using AccessManager.Services;
using AccessManager.Utills;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AccessManager.Controllers
{
    public class FileController : BaseController
    {
        private readonly UserService _userService;
        private readonly UnitService _unitService;
        private readonly FileService _fileService;

        public FileController(UserService userService, UnitService unitService, FileService fileService)
        {
            _userService = userService;
            _unitService = unitService;
            _fileService = fileService;
        }

        [HttpGet]
        public IActionResult UsersCsv()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(_fileService.GetUsersCsv(_userService.GetAccessibleUsers(loggedUser)).ToString());

            return File(bytes, "text/csv", "users.csv");
        }

        [HttpGet]
        public IActionResult UserAccessCsv()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(_fileService.GetUserAccessCsv(_userService.GetAccessibleUsers(loggedUser)).ToString());

            return File(bytes, "text/csv", "user_accesses.csv");
        }

        [HttpGet]
        public IActionResult UnitsCsv()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(_fileService.GetUnitsCsv(_unitService.GetUserUnits(loggedUser)).ToString());

            return File(bytes, "text/csv", "units.csv");
        }

        [HttpGet]

        public IActionResult AccessesCsv()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(_fileService.GetAccessesCsv().ToString());

            return File(bytes, "text/csv", "accesses.csv");
        }

        [HttpGet]

        public IActionResult UsersUnitsCsv()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(_fileService.GetUsersUnitsCsv(_unitService.GetUserUnits(loggedUser)).ToString());

            return File(bytes, "text/csv", "user_units.csv");
        }
    }
}
