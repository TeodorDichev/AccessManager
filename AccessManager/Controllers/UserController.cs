using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.UnitDepartment;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly PasswordService _passwordService;
        private readonly DepartmentUnitService _departmentUnitService;
        private readonly AccessService _informationSystemService;

        public UserController(Context context, PasswordService passwordService, UserService userService,
            AccessService informationSystemsService, AccessService accessService, DepartmentUnitService departmentUnitService)
        {
            _passwordService = passwordService;
            _userService = userService;
            _informationSystemService = informationSystemsService;
            _accessService = accessService;
            _departmentUnitService = departmentUnitService;
        }

        [HttpGet]
        public IActionResult UserList(string sortBy, string filterUnit, string filterDepartment, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            List<UserListItemViewModel> allUsers = _userService.GetFilteredUsers(sortBy, filterUnit, filterDepartment, loggedUser);
            int totalUsers = allUsers.Count();

            List<UserListItemViewModel> users = allUsers.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var model = new UserListViewModel
            {
                Users = users,
                SortOptions = _userService.GetSortOptions(),
                SelectedSortOption = sortBy,
                FilterUnits = loggedUser.AccessibleUnits.Select(au => au.Unit.Description).ToList(),
                SelectedFilterUnit = filterUnit,
                FilterDepartments = loggedUser.AccessibleUnits.Select(u => u.Unit.Department.Description).Distinct().ToList(),
                SelectedFilterDepartment = filterDepartment,
                HasWriteAuthority = (loggedUser.WritingAccess != AuthorityType.None),
                IsSuperAdmin = loggedUser.WritingAccess == AuthorityType.SuperAdmin,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null)
            {
                HttpContext.Session.Remove("PendingUser");
                return RedirectToAction("Login");
            }

            var pendingUserJson = HttpContext.Session.GetString("PendingUser");

            CreateUserViewModel model = new CreateUserViewModel
            {
                // TO DO SYSTEMS
                Departments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser)
            };

            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model, string redirectTo)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            if (_userService.UserWithUsernameExists(model.UserName))
                ModelState.AddModelError("UserName", "Потребител с това потребителско име вече съществува.");

            if (loggedUser.WritingAccess < model.SelectedWritingAccess || loggedUser.ReadingAccess < model.SelectedReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", "Не може да добавяш потребител с по-висок достъп.");

            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

            if (!ModelState.IsValid)
            {
                model.Departments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser);
                if (model.SelectedDepartmentId.HasValue)
                    model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

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
                UnitId = model.SelectedUnitId.Value,
                ReadingAccess = model.SelectedReadingAccess,
                WritingAccess = model.SelectedWritingAccess,
            };

            user.Password = _passwordService.HashPassword(user, model.Password);
            _userService.AddUser(user);

            if (model.SelectedReadingAccess >= AuthorityType.Full)
            {
                user.AccessibleUnits = _departmentUnitService.GetUnits().Select(u => new UnitUser
                {
                    Unit = u,
                    UnitId = u.Id,
                    UserId = user.Id,
                    User = user
                }).ToList();
            }
            else if (model.SelectedReadingAccess == AuthorityType.Restricted && !string.IsNullOrEmpty(model.SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = model.SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

            // TO DO SYSTEMS

            _userService.SaveChanges();
            if (redirectTo == "MapAccess") 
                return RedirectToAction("MapAccess", new { username = model.UserName });
            return RedirectToAction(redirectTo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUser(string username)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");
            if (string.IsNullOrWhiteSpace(username)) return BadRequest();

            var userToDelete = _userService.GetUser(username);
            if (userToDelete == null) return NotFound();
            if (!_userService.canUserEditUser(loggedUser, userToDelete)) return BadRequest();

            _userService.SoftDeleteUser(userToDelete);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HardDeleteUsers()
        {
            _userService.HardDeleteUsers();
            return RedirectToAction("UserList");
        }

        [HttpGet]
        public IActionResult GetAccessibleUnitsForUserDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");
            var res = loggedUser.AccessibleUnits.Where(au => au.Unit.DepartmentId == departmentId).Select(au => new { au.UnitId, au.Unit.Description });
            return Json(res);
        }

        [HttpGet]
        public IActionResult GetAccessibleDepartmentsForUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            var departments = loggedUser.AccessibleUnits
                .GroupBy(u => u.Unit.Department)
                .Select(g => new
                {
                    DepartmentId = g.Key.Id,
                    DepartmentName = g.Key.Description,
                    Units = g.Select(u => new
                    {
                        UnitId = u.UnitId,
                        UnitName = u.Unit.Description
                    }).ToList()
                })
                .ToList();

            return Json(departments);
        }

        [HttpGet]
        public IActionResult EditUser(string username)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

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
                SelectedDepartmentId = user.Unit.Department.Id,
                SelectedUnitId = user.Unit.Id,
                AccessibleUnits = user.AccessibleUnits
                    .Select(au => new UnitViewModel
                    {
                        DepartmentName = au.Unit.Department.Description,
                        UnitName = au.Unit.Description,
                        UnitId = au.Unit.Id
                    }).ToList(),
                UserAccesses = user.UserAccesses
                    .Select(ua => new AccessViewModel
                    {
                        Id = ua.AccessId,
                        //InformationSystemDescription = ua.Access.System.Name,
                        //InformationSystemId = ua.Access.System.Id,
                        ParentAccessDescription = ua.Access.ParentAccess?.Description ?? string.Empty,
                        Description = ua.Access.Description,
                        //Directive = ua.Directive,
                    }).ToList(),
                AvailableDepartments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser),
                AvailableUnits = _userService.GetAllowedUnitsForDepartmentAsSelectListItem(loggedUser, user.Unit.Department.Id),
                SelectedAccessibleUnitIds = string.Join(",", user.AccessibleUnits.Select(au => au.UnitId))
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
            if (!ModelState.IsValid) return View(model);

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.EGN = model.EGN;
            user.Phone = model.Phone;
            user.UserName = model.UserName;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.Password = _passwordService.HashPassword(user, model.NewPassword);
            }
            Unit? newUnit = _departmentUnitService.GetUnits().FirstOrDefault(u => u.Id == model.SelectedUnitId);
            if (newUnit != null)
            {
                user.UnitId = newUnit.Id;
                user.Unit = newUnit;
            }

            _userService.SaveChanges();
            return RedirectToAction("EditUser", new { UserName = model.UserName });
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(string username, Guid unitId)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            _departmentUnitService.RemoveUserUnit(user.Id, unitId);
            return RedirectToAction("EditUser", new { UserName = username });
        }

        [HttpPost]
        public IActionResult RemoveUserAccess(string username, Guid accessId)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            // TO DO SYSTEMS
            return RedirectToAction("EditUser", new { UserName = username });
        }

        [HttpPost]
        public IActionResult UpdateUserAccessDirective(string username, Guid accessId, string directive)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            // TO DO SYSTEMS
            return RedirectToAction("EditUser", new { UserName = username });
        }

        [HttpPost]
        public IActionResult AddUnitAccess(string username, List<Guid> selectedUnitIds)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            _departmentUnitService.AddUnitAccess(user.Id, selectedUnitIds);
            return RedirectToAction("EditUser", new { UserName = username });
        }

        [HttpPost]
        public IActionResult AddAccesses(string username, List<AccessViewModel> accesses)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            // TO DO SYSTEMS
            return RedirectToAction("EditUser", new { UserName = username });
        }
    }
}
