using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccountController : BaseController
    {
        private readonly Context context;
        private readonly PasswordService passwordService;
        public AccountController(Context context, PasswordService passwordService)
        {
            this.context = context;
            this.passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            User? user = context.Users.FirstOrDefault(u => u.UserName == model.Username);
            if (user != null && user.Password != null && passwordService.VerifyPassword(model.Password, user.Password))
            {
                HttpContext.Session.SetString("Username", model.Username);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Невалиден опит за вход!");
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.SetString("Username", "");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult MyProfile()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            if (context.Users.FirstOrDefault(u => u.UserName == username) == null)
                return NotFound();

            return View(BuildLoggedAccountViewModel(username));
        }

        [HttpPost]
        public IActionResult MyProfile(LoggedAccountViewModel model, string? OldPassword, string? NewPassword)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = context.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (string.IsNullOrWhiteSpace(OldPassword) || !passwordService.VerifyPassword(OldPassword, user.Password))
                {
                    ModelState.AddModelError("Password", "Старата парола е невалидна.");
                    return View(model);
                }

                user.Password = passwordService.HashPassword(NewPassword);
            }

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.EGN = model.EGN;
            user.Phone = model.Phone;

            if (user.WritingAccess >= AuthorityType.Full)
                user.UserName = model.UserName;

            context.SaveChanges();

            ViewBag.Success = true;
            return View(BuildLoggedAccountViewModel(user.UserName));
        }

        private LoggedAccountViewModel BuildLoggedAccountViewModel(string username)
        {
            var user = context.Users.FirstOrDefault(u => u.UserName == username);

            if (user == null) return null;

            return new LoggedAccountViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                ReadingAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(user.ReadingAccess),
                WritingAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(user.WritingAccess),
                UnitDescription = user.Unit.Description,
                DepartmentDescription = user.Unit.Department.Description,
                EGN = user.EGN ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                AccessibleUnits = user.AccessibleUnits.Select(u => u.Unit.Description).ToList(),
                UserAccesses = user.UserAccesses.Select(ua => ua.Access.Description).ToList(), // To be modified for tree structure
                canEdit = (user.WritingAccess != AuthorityType.None && user.WritingAccess != AuthorityType.None)
            };
        }
    }
}
