using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class HomeController : BaseController
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        public HomeController(UserService userService, PasswordService passwordService)
        {
            _userService = userService;
            _passwordService = passwordService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (_userService.GetUser(HttpContext.Session.GetString("Username")) != null) 
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (_userService.GetUser(HttpContext.Session.GetString("Username")) != null) ModelState.AddModelError("", ExceptionMessages.LoggedInLogInAttempt);

            User? user = _userService.GetUser(model.Username);
            if (user != null && user.Password != null && _passwordService.VerifyPassword(user, model.Password, user.Password))
            {
                HttpContext.Session.SetString("Username", model.Username);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", ExceptionMessages.InvalidLoginAttempt);
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
