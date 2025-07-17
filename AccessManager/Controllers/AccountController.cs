using AccessManager.Data;
using AccessManager.Data.Entities;
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
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            if (context.Users.FirstOrDefault(u => u.UserName == username) == null)
                return NotFound();

            return View(BuildLoggedAccountViewModel(username));
        }

        [HttpPost]
        public IActionResult Profile(LoggedAccountViewModel model, string? OldPassword, string? NewPassword)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Find user by UserName (we need actual entity to validate and update)
            var user = context.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (user == null)
                return NotFound();

            // Password check (only if new password is requested)
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (string.IsNullOrWhiteSpace(OldPassword) || !passwordService.VerifyPassword(OldPassword, user.Password))
                {
                    ModelState.AddModelError("Password", "Старата парола е невалидна.");
                    return View(model);
                }

                user.Password = passwordService.HashPassword(NewPassword);
            }

            // Update fields
            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.EGN = model.EGN;
            user.Phone = model.Phone;

            // Update username only if WritingAccess is full (in Bulgarian: "Пълен")
            if (model.WritingAccess == "Пълен")
                user.UserName = model.UserName;

            context.SaveChanges();

            // Rebuild the view model (in case the database has changed, or to preserve dependent fields)
            var updatedModel = new LoggedAccountViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                EGN = user.EGN ?? "",
                Phone = user.Phone ?? "",
                ReadingAccess = user.ReadingAccess.GetType().Name,
                WritingAccess = user.WritingAccess.GetType().Name,
                UnitDescription = user.Unit.Description,
                DepartmentDescription = user.Unit.Department.Description,
                AccessibleUnits = user.AccessibleUnits.Select(u => u.Unit.Description).ToList(),
                UserAccesses = user.UserAccesses.Select(a => a.Access.Description).ToList()
            };

            ViewBag.Success = true;
            return View(updatedModel);
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
                ReadingAccess = AccessLocalization.GetBulgarianReadingAccess(user.ReadingAccess),
                WritingAccess = AccessLocalization.GetBulgarianWritingAccess(user.WritingAccess),
                UnitDescription = user.Unit.Description,
                DepartmentDescription = user.Unit.Department.Description,
                EGN = user.EGN ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                AccessibleUnits = user.AccessibleUnits.Select(u => u.Unit.Description).ToList(),
                UserAccesses = user.UserAccesses.Select(ua => ua.Access.Description).ToList(), // To be modified for tree structure
                canEditUserName = user.ReadingAccess == Data.Enums.ReadingAccess.Full
            };
        }
    }
}
