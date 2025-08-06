using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.UnitDepartment;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        public AccountController(UserService userService, PasswordService passwordService)
        {
            _userService = userService;
            _passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if(_userService.GetUser(HttpContext.Session.GetString("Username")) != null)  ModelState.AddModelError("", "Излесте от профила си!");

            User? user = _userService.GetUser(model.Username);
            if (user != null && user.Password != null && _passwordService.VerifyPassword(user, model.Password, user.Password))
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
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            LoggedAccountViewModel model = new LoggedAccountViewModel
            {
                UserName = loggedUser.UserName,
                FirstName = loggedUser.FirstName,
                MiddleName = loggedUser.MiddleName,
                LastName = loggedUser.LastName,
                ReadingAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(loggedUser.ReadingAccess),
                WritingAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(loggedUser.WritingAccess),
                UnitDescription = loggedUser.Unit.Description,
                DepartmentDescription = loggedUser.Unit.Department.Description,
                EGN = loggedUser.EGN ?? string.Empty,
                Phone = loggedUser.Phone ?? string.Empty,
                AccessibleUnits = loggedUser.AccessibleUnits.Select(au => new UnitViewModel 
                    { 
                        UnitId = au.UnitId, 
                        UnitName = au.Unit.Description, 
                        DepartmentName = au.Unit.Department.Description
                    }).ToList(),
                UserAccesses = loggedUser.UserAccesses.Select(ua => new AccessViewModel
                    {
                        Id = ua.AccessId,
                        //InformationSystemDescription = ua.Access.System.Name,
                        Description = ua.Access.Description,
                        IsSelected = true,
                        //Directive = ua.Directive,
                        ParentAccessDescription = ua.Access.ParentAccess?.Description ?? "-",
                }).ToList(),
                canEdit = (loggedUser.WritingAccess != AuthorityType.None && loggedUser.WritingAccess != AuthorityType.None)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Profile(string username)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            LoggedAccountViewModel model = new LoggedAccountViewModel
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
                AccessibleUnits = user.AccessibleUnits.Select(au => new UnitViewModel
                {
                    UnitId = au.UnitId,
                    UnitName = au.Unit.Description,
                    DepartmentName = au.Unit.Department.Description
                }).ToList(),
                UserAccesses = user.UserAccesses.Select(ua => new AccessViewModel
                {
                    Id = ua.AccessId,
                    //InformationSystemDescription = ua.Access.System.Name,
                    Description = ua.Access.Description,
                    IsSelected = true,
                    //Directive = ua.Directive,
                    ParentAccessDescription = ua.Access.ParentAccess?.Description ?? "-",
                }).ToList(),
                canEdit = (user.WritingAccess != AuthorityType.None && user.WritingAccess != AuthorityType.None)
            };

            return View("MyProfile", model);
        }

        [HttpPost]
        public IActionResult MyProfile(LoggedAccountViewModel model, string? OldPassword, string? NewPassword)
        {
            if (!ModelState.IsValid) return View(model);

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            if (!string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(loggedUser.Password))
            {
                if (string.IsNullOrWhiteSpace(OldPassword) || !_passwordService.VerifyPassword(loggedUser, OldPassword, loggedUser.Password))
                {
                    ModelState.AddModelError("Password", "Старата парола е невалидна.");
                    return View(model);
                }

                loggedUser.Password = _passwordService.HashPassword(loggedUser, NewPassword);
            }

            loggedUser.FirstName = model.FirstName;
            loggedUser.MiddleName = model.MiddleName;
            loggedUser.LastName = model.LastName;
            loggedUser.EGN = model.EGN;
            loggedUser.Phone = model.Phone;

            if (loggedUser.WritingAccess >= AuthorityType.Full) loggedUser.UserName = model.UserName;

            _userService.SaveChanges();
            ViewBag.Success = true;
            return View(model);
        }
    }
}
