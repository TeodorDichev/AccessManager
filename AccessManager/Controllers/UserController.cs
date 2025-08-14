using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Unit;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;
        private readonly PasswordService _passwordService;
        private readonly DepartmentService _departmentService;
        private readonly UnitService _unitService;

        public UserController(Context context, PasswordService passwordService, UserService userService,
            AccessService accessService, DepartmentService departmentService, DirectiveService directiveService, UnitService unitService)
        {
            _passwordService = passwordService;
            _userService = userService;
            _accessService = accessService;
            _departmentService = departmentService;
            _directiveService = directiveService;
            _unitService = unitService;
        }

        [HttpGet]
        public IActionResult MyProfile()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            MyProfileViewModel model = new()
            {
                UserName = loggedUser.UserName,
                FirstName = loggedUser.FirstName,
                MiddleName = loggedUser.MiddleName,
                LastName = loggedUser.LastName,
                ReadingAccess = loggedUser.ReadingAccess,
                WritingAccess = loggedUser.WritingAccess,
                EGN = loggedUser.EGN ?? string.Empty,
                Phone = loggedUser.Phone ?? string.Empty,
                AccessibleUnits = loggedUser.AccessibleUnits.Select(au => new UnitViewModel
                {
                    UnitId = au.UnitId,
                    UnitName = au.Unit.Description,
                    DepartmentName = au.Unit.Department.Description
                }).ToList(),
                AvailableDepartments = _userService.GetAllowedDepartments(loggedUser)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description }).ToList(),
                AvailableUnits = _userService.GetAllowedUnitsForDepartment(loggedUser, loggedUser.Unit.Department.Id)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description }).ToList(),
                SelectedDepartmentId = loggedUser.Unit.Department.Id,
                SelectedUnitId = loggedUser.Unit.Id,
                UserAccesses = _accessService.GetGrantedUserAccesses(loggedUser)
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult MyProfile(MyProfileViewModel model, string? OldPassword, string? NewPassword)
        {
            if (!ModelState.IsValid) return View(model);

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (!string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(loggedUser.Password))
            {
                if (string.IsNullOrWhiteSpace(OldPassword) || !_passwordService.VerifyPassword(loggedUser, OldPassword, loggedUser.Password))
                {
                    ModelState.AddModelError("Password", "Старата парола е невалидна.");
                    return View(model);
                }

                loggedUser.Password = _passwordService.HashPassword(loggedUser, NewPassword);
            }

            _userService.UpdateUser(model, loggedUser);
            return RedirectToAction("MyProfile");
        }

        [HttpGet]
        public IActionResult UserList(string sortBy, string filterUnit, string filterDepartment, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            List<UserListItemViewModel> allUsers = _userService.GetFilteredUsers(sortBy, filterUnit, filterDepartment, loggedUser);
            int totalUsers = allUsers.Count();

            List<UserListItemViewModel> users = allUsers.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var model = new UserListViewModel
            {
                Users = users,
                SortOptions = _userService.GetSortOptions(),
                SelectedSortOption = sortBy,
                SelectedFilterUnit = filterUnit,
                FilterDepartments = loggedUser.AccessibleUnits.Select(u => u.Unit.Department.Description).Distinct().ToList(),
                SelectedFilterDepartment = filterDepartment,
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage)
            };

            if (string.IsNullOrEmpty(filterDepartment))
            {
                model.FilterUnits = loggedUser.AccessibleUnits.Select(au => au.Unit.Description).ToList();
            }
            else
            {
                model.FilterUnits = loggedUser.AccessibleUnits.Where(au => au.Unit.Department.Description == filterDepartment).Select(au => au.Unit.Description).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            CreateUserViewModel model = new CreateUserViewModel
            {
                Departments = _userService.GetAllowedDepartments(loggedUser).Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Description,
                }).ToList(),
            };

            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsForDepartment(loggedUser, loggedUser.Unit.Department.Id)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description }).ToList();
            ;

            return View(model);
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

            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsForDepartment(loggedUser, loggedUser.Unit.Department.Id)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description }).ToList();

            if (!ModelState.IsValid)
            {
                model.Departments = _userService.GetAllowedDepartments(loggedUser).Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Description,
                }).ToList();

                if (model.SelectedDepartmentId.HasValue)
                    model.Units = _userService.GetAllowedUnitsForDepartment(loggedUser, loggedUser.Unit.Department.Id)
                        .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description }).ToList();

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
                UnitId = model.SelectedUnitId,
                ReadingAccess = model.SelectedReadingAccess,
                WritingAccess = model.SelectedWritingAccess,
            };

            user.Password = _passwordService.HashPassword(user, model.Password);
            _userService.AddUser(user);

            if (model.SelectedReadingAccess >= AuthorityType.Full)
                _unitService.AddFullUnitAccess(user.Id);

            if (redirectTo == "MapUserAccess")
                return RedirectToAction(redirectTo, new { username = model.UserName });
            else if (redirectTo == "MapUserUnitAccess")
                return RedirectToAction("MapUserUnitAccess", new { username = model.UserName });

            return RedirectToAction(redirectTo);
        }

        [HttpGet]
        public IActionResult EditUser(string username)
        {
            if (HttpContext.Session.GetString("Username") == username)
                return RedirectToAction("MyProfile");

            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (loggedUser.WritingAccess < user.WritingAccess)
            {
                ViewBag.IsReadOnly = true;
            }

            var model = new EditUserViewModel
            {
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
                SelectedUnitId = user.Unit.Id,
                AvailableDepartments = _userService.GetAllowedDepartments(loggedUser)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description }).ToList(),
                AvailableUnits = _userService.GetAllowedUnitsForDepartment(loggedUser, user.Unit.Department.Id)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description }).ToList(),
            };
            model.AvailableDepartments.ForEach(d => d.Selected = d.Value == model.SelectedDepartmentId.ToString());
            model.AvailableUnits.ForEach(d => d.Selected = d.Value == model.SelectedUnitId.ToString());
            return View(model);
        }

        [HttpPost]
        public IActionResult EditUser(EditUserViewModel model)
        {
            var user = _userService.GetUser(model.UserName);
            if (user == null) return BadRequest();

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (user.UserName != model.UserName && _userService.UserWithUsernameExists(model.UserName))
                ModelState.AddModelError("UserName", "Потребител с това потребителско име вече съществува.");

            if (loggedUser.WritingAccess < model.WritingAccess || loggedUser.ReadingAccess < model.ReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", "Не може да добавяш потребител с по-висок достъп.");

            if (!ModelState.IsValid) return View(model);

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = _passwordService.HashPassword(user, model.NewPassword);
            }

            _userService.UpdateUser(model, user);
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public IActionResult SoftDeleteUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (string.IsNullOrWhiteSpace(username)) return BadRequest();

            var userToDelete = _userService.GetUser(username);
            if (userToDelete == null) return NotFound();

            if (loggedUser.WritingAccess < AuthorityType.Full
                || userToDelete.WritingAccess >= loggedUser.WritingAccess
                || userToDelete.ReadingAccess >= loggedUser.WritingAccess) return BadRequest();

            _userService.SoftDeleteUser(userToDelete);
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public IActionResult HardDeleteUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (string.IsNullOrWhiteSpace(username)) return BadRequest();

            var userToDelete = _userService.GetDeletedUser(username);
            if (userToDelete == null) return NotFound();

            if (loggedUser.WritingAccess < AuthorityType.Full
                || userToDelete.WritingAccess >= loggedUser.WritingAccess
                || userToDelete.ReadingAccess >= loggedUser.WritingAccess) return BadRequest();

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
            if (userToRestore == null) return NotFound();

            _userService.RestoreUser(userToRestore);
            return RedirectToAction("DeletedUsers");
        }

        [HttpPost]
        public IActionResult RestoreAllUsers()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            _userService.RestoreAllUsers();
            return RedirectToAction("DeletedUsers");
        }

        [HttpGet]
        public IActionResult DeletedUsers(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin) return RedirectToAction("UserList");

            List<UserListItemViewModel> allUsers = _userService.GetDeletedUsers();
            int totalUsers = allUsers.Count();

            List<UserListItemViewModel> users = allUsers.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var model = new DeletedUserListViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage)
            };

            return View(model);
        }
    }
}
