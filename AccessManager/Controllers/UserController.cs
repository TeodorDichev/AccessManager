using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly UserAccessService _userAccessService;
        private readonly DirectiveService _directiveService;
        private readonly PasswordService _passwordService;
        private readonly DepartmentService _departmentService;
        private readonly UnitService _unitService;
        private readonly LogService _logService;

        public UserController(PasswordService passwordService, UserService userService, LogService logService,
            AccessService accessService, DepartmentService departmentService, DirectiveService directiveService, UnitService unitService, UserAccessService userAccessService)
        {
            _logService = logService;
            _passwordService = passwordService;
            _userService = userService;
            _accessService = accessService;
            _departmentService = departmentService;
            _directiveService = directiveService;
            _unitService = unitService;
            _userAccessService = userAccessService;
        }

        [HttpGet]
        public IActionResult SearchPositions(string term)
        {
            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var results = _userService.GetPositions()
                .Where(u => string.IsNullOrEmpty(term) || u.Description.ToLowerInvariant().Contains(termLower))
                .Select(u => new { id = u.Id, text = u.Description })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public IActionResult SearchUsers(string term)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var results = _userService.GetAccessibleUsers(loggedUser)
                .Where(u => string.IsNullOrEmpty(term) || u.UserName.ToLowerInvariant().Contains(termLower))
                .Select(u => new { id = u.Id, text = u.UserName })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public IActionResult MyProfile()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin;

            MyProfileViewModel model = new()
            {
                Id = loggedUser.Id,
                UserName = loggedUser.UserName,
                FirstName = loggedUser.FirstName,
                MiddleName = loggedUser.MiddleName,
                LastName = loggedUser.LastName,
                ReadingAccess = loggedUser.ReadingAccess,
                WritingAccess = loggedUser.WritingAccess,
                EGN = loggedUser.EGN ?? string.Empty,
                Phone = loggedUser.Phone ?? string.Empty,
                SelectedDepartmentDescription = loggedUser.Unit.Department.Description,
                SelectedDepartmentId = loggedUser.Unit.Department.Id,
                SelectedUnitDescription = loggedUser.Unit.Description,
                SelectedUnitId = loggedUser.Unit.Id,
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult MyProfile(MyProfileViewModel model, string? OldPassword, string? NewPassword)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (model.UserName != loggedUser.UserName && _userService.UserWithUsernameExists(model.UserName)) 
                ModelState.AddModelError("UserName", ExceptionMessages.InvalidUsername);

            if (model.SelectedDepartmentId == null) model.SelectedDepartmentDescription = "";
            if (model.SelectedUnitId == null) model.SelectedUnitDescription = "";

            if (!ModelState.IsValid) return View(model);

            if (!string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(loggedUser.Password))
            {
                if (string.IsNullOrWhiteSpace(OldPassword) || !_passwordService.VerifyPassword(loggedUser, OldPassword, loggedUser.Password))
                {
                    ModelState.AddModelError("Password", ExceptionMessages.InvalidPassword);
                    return View(model);
                }

                loggedUser.Password = _passwordService.HashPassword(loggedUser, NewPassword);
            }

            _userService.UpdateUser(model, loggedUser);
            _logService.AddLog(loggedUser, LogAction.Edit, loggedUser);
            TempData["Success"] = "Профилът беше обновен успешно";
            return RedirectToAction("MyProfile");
        }

        [HttpGet]
        public IActionResult GetAccessibleUnits(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessibleUnits = loggedUser.AccessibleUnits.Select(au => new UnitDepartmentViewModel
            {
                UnitName = au.Unit.Description,
                DepartmentName = au.Unit.Department.Description
            }).ToList();

            var totalCount = accessibleUnits.Count();

            var paged = accessibleUnits
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

            var result = new PagedResult<UnitDepartmentViewModel>
            {
                Items = paged,
                Page = page,
                TotalCount = totalCount
            };
            return PartialView("_AccessibleUnitsTable", result);
        }

        [HttpGet]
        public IActionResult GetUserAccesses(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");


            var result = _accessService.GetAccessesGrantedToUserPaged(loggedUser, null, page);

            return PartialView("_UserAccessesTable", result);
        }

        [HttpGet]
        public IActionResult UserList(UserListViewModel model, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var unit = _unitService.GetUnit(model.FilterUnitId);
            var department = _departmentService.GetDepartment(model.FilterDepartmentId);

            var result = new UserListViewModel
            {
                Users = _userService.GetAccessibleUsersPaged(loggedUser, unit, department, page, model.SelectedSortOption),
                SelectedSortOption = model.SelectedSortOption,
                FilterUnitId = model.FilterUnitId,
                FilterDepartmentId = model.FilterDepartmentId,
                FilterUnitDescription = unit?.Description ?? "",
                FilterDepartmentDescription = department?.Description ?? "",
                WriteAuthority = loggedUser.WritingAccess,
            };

            return View(result);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            return View(new CreateUserViewModel());
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model, string redirectTo)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (_userService.UserWithUsernameExists(model.UserName))
                ModelState.AddModelError("UserName", "Потребител с това потребителско име вече съществува.");

            if (loggedUser.WritingAccess < model.SelectedWritingAccess || loggedUser.ReadingAccess < model.SelectedReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", "Не може да добавяш потребител с по-висок достъп.");

            if (model.SelectedDepartmentId == null) model.SelectedDepartmentDescription = "";
            if (model.SelectedUnitDescription == null) model.SelectedDepartmentDescription = "";

            if (!ModelState.IsValid) return View(model);

            var unit = _unitService.GetUnit(model.SelectedUnitId);
            if (unit == null) 
            {
                ModelState.AddModelError("SelectedUnitId", "Отделът не съществува");
                return View(model);
            }

            var user = new User
            {
                UserName = model.UserName,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                EGN = model.EGN,
                Phone = model.Phone,
                UnitId = unit.Id,
                Unit = unit,
                ReadingAccess = model.SelectedReadingAccess,
                WritingAccess = model.SelectedWritingAccess,
            };

            user.Password = _passwordService.HashPassword(user, model.Password);
            _userService.AddUser(user);

            if (model.SelectedReadingAccess >= AuthorityType.Full)
                _unitService.AddAllUnitUsers(user);

            _logService.AddLog(loggedUser, LogAction.Add, user);

            if (redirectTo == "MapUserAccess")
                return RedirectToAction(redirectTo, new { username = model.UserName });
            else if (redirectTo == "MapUserUnitAccess")
                return RedirectToAction("MapUserUnitAccess", new { username = model.UserName });

            return RedirectToAction(redirectTo);
        }

        [HttpGet]
        public IActionResult EditUser(Guid userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            else if (loggedUser.Id == userId) return RedirectToAction("MyProfile");

            var user = _userService.GetUser(userId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("UserList");
            }

            ViewBag.IsReadOnly = loggedUser.WritingAccess < user.WritingAccess;

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                EGN = user.EGN ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                ReadingAccess = user.ReadingAccess,
                WritingAccess = user.WritingAccess,
                LoggedUserReadingAccess = loggedUser.ReadingAccess,
                LoggedUserWritingAccess = loggedUser.WritingAccess,
                SelectedDepartmentId = user.Unit.Department.Id,
                SelectedDepartmentDescription = user.Unit.Department.Description ?? "",
                SelectedUnitId = user.Unit.Id,
                SelectedUnitDescription = user.Unit.Description ?? "" 
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult EditUser(EditUserViewModel model)
        {
            var user = _userService.GetUser(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("UserList");
            }

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (user.UserName != model.UserName && _userService.UserWithUsernameExists(model.UserName))
                ModelState.AddModelError("UserName", ExceptionMessages.InvalidUsername);

            if (loggedUser.WritingAccess < model.WritingAccess || loggedUser.ReadingAccess < model.ReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", "Не може да добавяш потребител с по-висок достъп.");

            if (model.SelectedDepartmentId == null) model.SelectedDepartmentDescription = "";
            if (model.SelectedUnitId == null) model.SelectedUnitDescription = "";

            if (!ModelState.IsValid) return View(model);

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.Password = _passwordService.HashPassword(user, model.NewPassword);

            _userService.UpdateUser(model, user);
            _logService.AddLog(loggedUser, LogAction.Edit, user);

            return RedirectToAction("EditUser", new { user.Id });
        }

        [HttpPost]
        public IActionResult SoftDeleteUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (string.IsNullOrWhiteSpace(username)) return BadRequest();

            var userToDelete = _userService.GetUser(username);
            if (userToDelete == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("UserList");
            }
            else if (loggedUser.WritingAccess < AuthorityType.Full
                || userToDelete.WritingAccess >= loggedUser.WritingAccess
                || userToDelete.ReadingAccess >= loggedUser.WritingAccess)
            {
                TempData["Error"] = "Недостатъчен достъп!";
                return RedirectToAction("UserList");
            }
            else if (!_userService.CanDeleteUser(userToDelete))
            {
                TempData["Error"] = "Не може да изтриете този потребител, защото той участва в други записи!";
                return RedirectToAction("UserList");
            }

            TempData["Success"] = "Потребителят е изтрит успешно.";
            _logService.AddLog(loggedUser, LogAction.Delete, userToDelete);
            _userService.SoftDeleteUser(userToDelete);
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public IActionResult HardDeleteUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var userToDelete = _userService.GetDeletedUser(username);
            if (userToDelete == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("UserList");
            }

            if (loggedUser.WritingAccess < AuthorityType.Full
                || userToDelete.WritingAccess >= loggedUser.WritingAccess
                || userToDelete.ReadingAccess >= loggedUser.WritingAccess)
            {
                TempData["Error"] = "Недостатъчен достъп!";
                return RedirectToAction("UserList");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, userToDelete);
            _userService.HardDeleteUser(userToDelete);
            return RedirectToAction("DeletedUsers");
        }

        [HttpPost]
        public IActionResult HardDeleteUsers()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (loggedUser.WritingAccess == AuthorityType.SuperAdmin) _userService.HardDeleteUsers();
            return RedirectToAction("DeletedUsers");
        }

        [HttpPost]
        public IActionResult RestoreUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var userToRestore = _userService.GetDeletedUser(username);
            if (userToRestore == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("UserList");
            }

            _userService.RestoreUser(userToRestore);
            _logService.AddLog(loggedUser, LogAction.Edit, userToRestore);

            return RedirectToAction("DeletedUsers");
        }

        [HttpGet]
        public IActionResult DeletedUsers(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin) return RedirectToAction("UserList");


            var result = _userService.GetDeletedUsersPaged(page);
            return View(result);
        }

        [HttpGet]
        public IActionResult GetAccessibleUsers(string q = "")
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var all = _userService.GetAccessibleUsers(loggedUser).Append(loggedUser).Select(a => new { a.Id, a.UserName }).ToList();
            var qLower = (q ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => string.IsNullOrEmpty(qLower) || a.UserName.ToLowerInvariant().Contains(qLower))
                .OrderBy(a => a.UserName)
                .Take(30)
                .Select(a => new { id = a.Id, text = a.UserName })
                .ToList();

            return Json(candidates);
        }
    }
}
