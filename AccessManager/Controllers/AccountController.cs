using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccountController : Controller
    {
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

            // TODO: Authenticate user (e.g., check from database)

            if (model.Username == "admin" && model.Password == "pass")
            {
                // Login success — redirect to dashboard/home
                HttpContext.Session.SetString("Username", model.Username);

                // Then redirect
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Невалиден опит за вход!");
            return View(model);
        }
    }
}
