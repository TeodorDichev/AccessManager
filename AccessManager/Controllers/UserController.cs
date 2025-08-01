using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.InformationSystem;
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
        private readonly InformationSystemsService _informationSystemService;

        public UserController(Context context, PasswordService passwordService, UserService userService,
            InformationSystemsService informationSystemsService, AccessService accessService, DepartmentUnitService departmentUnitService)
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
            CreateUserViewModel model;

            if (!string.IsNullOrEmpty(pendingUserJson)) model = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson) ?? new CreateUserViewModel();
            else model = new CreateUserViewModel();


            model.Departments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser);
            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            if (_userService.UserWithUsernameExists(model.UserName)) 
                    ModelState.AddModelError("UserName", "Потребител с това потребителско име вече съществува.");

            if(loggedUser.WritingAccess < model.SelectedWritingAccess || loggedUser.ReadingAccess < model.SelectedReadingAccess)
                ModelState.AddModelError("SelectedReadingAccess", "Не може да добавяш потребител с по-висок достъп.");

            if (!ModelState.IsValid)
            {
                model.Departments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser);
                if (model.SelectedDepartmentId.HasValue)
                    model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

                return View(model);
            }

            HttpContext.Session.SetString("PendingUser", JsonSerializer.Serialize(model));
            return RedirectToAction("MapUserToInformationSystems");
        }

        [HttpGet]
        public IActionResult MapUserToInformationSystems()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null)
            {
                HttpContext.Session.Remove("PendingUser");
                return RedirectToAction("Login");
            }

            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(pendingUserJson)) return RedirectToAction("CreateUser");

            var createUserModel = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson);
            if (createUserModel == null) return RedirectToAction("CreateUser");

            var systems = _informationSystemService.GetAllInformationSystems();

            var model = new MapUserToSystemsViewModel
            {
                UserName = createUserModel.UserName,
                FirstName = createUserModel.FirstName,
                LastName = createUserModel.LastName,
                DepartmentDescription = _departmentUnitService.GetDepartmentDescription(createUserModel.SelectedUnitId),
                UnitDescription = _departmentUnitService.GetUnitDescription(createUserModel.SelectedUnitId),
                Systems = systems.Select(s => new InformationSystemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Accesses = s.Accesses
                        .Where(a => a.DeletedOn == null && a.ParentAccessId == null)
                        .Select(a => new AccessViewModel
                        {
                            Id = a.Id,
                            Description = a.Description,
                            SubAccesses = a.SubAccesses
                                .Where(sa => sa.DeletedOn == null)
                                .Select(sa => new AccessViewModel
                                {
                                    Id = sa.Id,
                                    Description = sa.Description
                                }).ToList()
                        }).ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUserFromSession()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null)
            {
                HttpContext.Session.Remove("PendingUser");
                return RedirectToAction("Login");
            }

            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(pendingUserJson)) return RedirectToAction("CreateUser");

            var createUserModel = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson);
            if (createUserModel == null) return RedirectToAction("CreateUser");

            createUserModel.Departments = _userService.GetAllowedDepartmentsAsSelectListItem(loggedUser);
            if (createUserModel.SelectedDepartmentId.HasValue)
                createUserModel.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

            return View("CreateUser", createUserModel);
        }

        [HttpGet]
        public IActionResult DiscardCreateUserAction()
        {
            HttpContext.Session.Remove("PendingUser");
            return RedirectToAction("UserList");
        }

        [HttpPost]
        public IActionResult MapUserToInformationSystems(MapUserToSystemsViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null)
            {
                HttpContext.Session.Remove("PendingUser");
                return RedirectToAction("Login");
            }

            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(pendingUserJson)) return RedirectToAction("CreateUser");

            var createUserModel = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson);
            if (createUserModel == null || !createUserModel.SelectedUnitId.HasValue) return RedirectToAction("CreateUser");

            var user = new User
            {
                UserName = createUserModel.UserName,
                FirstName = createUserModel.FirstName,
                MiddleName = createUserModel.MiddleName,
                LastName = createUserModel.LastName,
                EGN = createUserModel.EGN,
                Phone = createUserModel.Phone,
                UnitId = createUserModel.SelectedUnitId.Value,
                ReadingAccess = createUserModel.SelectedReadingAccess,
                WritingAccess = createUserModel.SelectedWritingAccess,
            };

            user.Password = _passwordService.HashPassword(user, createUserModel.Password);

            if (!string.IsNullOrEmpty(createUserModel.SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = createUserModel.SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

            _userService.AddUser(user);
            foreach (var system in model.Systems.Where(s => s.IsSelected))
            {
                foreach (var access in system.Accesses.Where(a => a.IsSelected))
                {
                    _accessService.AddUserAccess(new UserAccess
                    {
                        UserId = user.Id,
                        AccessId = access.Id,
                        Directive = access.Directive ?? system.Directive ?? string.Empty,
                        GrantedOn = DateTime.UtcNow
                    });

                    foreach (var sub in access.SubAccesses.Where(s => s.IsSelected))
                    {
                        _accessService.AddUserAccess(new UserAccess
                        {
                            UserId = user.Id,
                            AccessId = sub.Id,
                            Directive = sub.Directive ?? access.Directive ?? system.Directive ?? string.Empty,
                            GrantedOn = DateTime.UtcNow
                        });
                    }
                }
            }

            _userService.SaveChanges();
            HttpContext.Session.Remove("PendingUser");
            return RedirectToAction("UserList");
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

            return Json(loggedUser.AccessibleUnits.Select(au => new { au.UnitId, au.Unit.Description }));
        }
    }
}
