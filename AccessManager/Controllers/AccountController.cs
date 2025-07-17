using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccountController : Controller
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
    }
}
