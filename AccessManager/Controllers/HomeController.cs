using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Models;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AccessManager.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(username))
                ViewData["Username"] = username;

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
