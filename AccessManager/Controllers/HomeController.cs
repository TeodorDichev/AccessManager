using System.Diagnostics;
using AccessManager.Models;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
            {
                ViewData["Username"] = username;

                var loggedAccountViewModel = new LoggedAccountViewModel
                {
                    Username = username
                };

                return View(loggedAccountViewModel);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
