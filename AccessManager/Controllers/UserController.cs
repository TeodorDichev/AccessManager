using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly PasswordService _passwordService;
        private readonly DepartmentService _departmentService;
        private readonly UnitService _unitService;
        private readonly LogService _logService;
        private readonly PositionService _positionService;

        public UserController(PasswordService passwordService, UserService userService, LogService logService,
            AccessService accessService, DepartmentService departmentService, UnitService unitService, PositionService positionService)
        {
            _logService = logService;
            _passwordService = passwordService;
            _userService = userService;
            _accessService = accessService;
            _departmentService = departmentService;
            _unitService = unitService;
            _positionService = positionService;
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
                SelectedPositionId = loggedUser.PositionId,
                SelectedPositionDescription = loggedUser.Position?.Description ?? "",
                // For safety if changed inside the view
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
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
            if (model.SelectedPositionId == null) model.SelectedPositionDescription = "";

            model.LoggedUserReadAuthority = loggedUser.ReadingAccess;
            model.LoggedUserWriteAuthority = loggedUser.WritingAccess;

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

            var unit = _unitService.GetUnit(model.FilterUnitId);
            var department = _departmentService.GetDepartment(model.FilterDepartmentId);
            var position = _positionService.GetPosition(model.FilterPositionId);

            var result = new UserListViewModel
            {
                Users = _userService.GetAccessibleUsersPaged(loggedUser, unit, department, position, page, model.SelectedSortOption),
                SelectedSortOption = model.SelectedSortOption,
                FilterUnitId = model.FilterUnitId,
                FilterDepartmentId = model.FilterDepartmentId,
                FilterPositionId = model.FilterPositionId,
                FilterUnitDescription = unit?.Description ?? "",
                FilterDepartmentDescription = department?.Description ?? "",
                FilterPositionDescription = position?.Description ?? "",
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess
            };

            return View(result);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.Restricted)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }

            var model = new CreateUserViewModel()
            {
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model, string redirectTo)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (_userService.UserWithUsernameExists(model.UserName))
                ModelState.AddModelError("UserName", ExceptionMessages.InvalidUsername);

            if (loggedUser.WritingAccess < model.SelectedWritingAccess || loggedUser.ReadingAccess < model.SelectedReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", ExceptionMessages.InsufficientAuthority);

            if (model.SelectedDepartmentId == null) model.SelectedDepartmentDescription = "";
            if (model.SelectedPositionId == null) model.SelectedPositionDescription = "";
            if (model.SelectedUnitDescription == null) model.SelectedDepartmentDescription = "";

            var unit = _unitService.GetUnit(model.SelectedUnitId);
            if (unit == null) ModelState.AddModelError("SelectedUnitId", ExceptionMessages.UnitNotFound);
            else
            {
                ModelState["SelectedUnitDescription"]!.ValidationState = ModelValidationState.Valid;
                ModelState["SelectedDepartmentDescription"]!.ValidationState = ModelValidationState.Valid;
            }

            var position = _positionService.GetPosition(model.SelectedPositionId);
            if (position == null) ModelState.AddModelError("SelectedPositionId", ExceptionMessages.PositionNotFound);
            else ModelState["SelectedPositionDescription"]!.ValidationState = ModelValidationState.Valid;
               
            if(string.IsNullOrEmpty(model.Password) 
                && (model.SelectedReadingAccess > AuthorityType.None || model.SelectedWritingAccess > AuthorityType.None))
                ModelState.AddModelError("Password", ExceptionMessages.RequiredField);

            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = model.UserName,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                PositionId = position!.Id,
                Position = position,
                EGN = model.EGN,
                Phone = model.Phone,
                UnitId = unit!.Id,
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
                return RedirectToAction(redirectTo, new { userId = user.Id });
            else if (redirectTo == "MapUserUnitAccess")
                return RedirectToAction("MapUserUnitAccess", new { userId = user.Id });
            else 
                return RedirectToAction("UserList");
        }

        [HttpGet]
        public IActionResult EditUser(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            else if (loggedUser.Id == userId) return RedirectToAction("MyProfile");

            var user = _userService.GetUser(userId);
            if (user == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
                return RedirectToAction("UserList");
            }

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
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                SelectedDepartmentId = user.Unit.Department.Id,
                SelectedDepartmentDescription = user.Unit.Department.Description ?? "",
                SelectedUnitId = user.Unit.Id,
                SelectedUnitDescription = user.Unit.Description ?? "",
                SelectedPositionId = user.Position?.Id ?? null,
                SelectedPositionDescription = user.Position?.Description ?? "-"
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
                ModelState.AddModelError("SelectedReadingAccess", ExceptionMessages.InsufficientAuthority);

            if (model.SelectedDepartmentId == null) model.SelectedDepartmentDescription = "";
            if (model.SelectedUnitId == null) model.SelectedUnitDescription = "";
            if (model.SelectedPositionId == null) model.SelectedPositionDescription = "";

            if (!ModelState.IsValid) return View(model);

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
                user.Password = _passwordService.HashPassword(user, model.NewPassword);

            _userService.UpdateUser(model, user);
            _logService.AddLog(loggedUser, LogAction.Edit, user);

            return RedirectToAction("EditUser", new { userId = model.UserId });
        }

        [HttpPost]
        public IActionResult SoftDeleteUser(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var userToDelete = _userService.GetUser(userId);
            if (userToDelete == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
                return RedirectToAction("UserList");
            }
            else if (loggedUser.WritingAccess < AuthorityType.Full
                || userToDelete.WritingAccess >= loggedUser.WritingAccess
                || userToDelete.ReadingAccess >= loggedUser.WritingAccess)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }
            else if (!_userService.CanDeleteUser(userToDelete))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeDeletedDueToDependencies;
                return RedirectToAction("UserList");
            }

            TempData["Success"] = "Потребителят е изтрит успешно.";
            _logService.AddLog(loggedUser, LogAction.Delete, userToDelete);
            _userService.SoftDeleteUser(userToDelete);
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public IActionResult HardDeleteUser(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }

            var userToDelete = _userService.GetDeletedUser(userId);
            if (userToDelete == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
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
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }

            _userService.HardDeleteUsers();
            return RedirectToAction("DeletedUsers");
        }

        [HttpPost]
        public IActionResult RestoreUser(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }

            var userToRestore = _userService.GetDeletedUser(userId);
            if (userToRestore == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
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
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UserList");
            }

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
