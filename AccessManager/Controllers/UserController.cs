using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.UnitDepartment;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

            if (model.SelectedDepartmentId.HasValue)
                model.Units = _userService.GetAllowedUnitsAsSelectListItem(loggedUser);

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
            _userService.AddUser(user);

            if (createUserModel.SelectedReadingAccess >= AuthorityType.Full)
            {
                user.AccessibleUnits = _departmentUnitService.GetUnits().Select(u => new UnitUser
                {
                    Unit = u,
                    UnitId = u.Id,
                    UserId = user.Id,
                    User = user
                }).ToList();
            }
            else if (createUserModel.SelectedReadingAccess == AuthorityType.Restricted && !string.IsNullOrEmpty(createUserModel.SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = createUserModel.SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

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

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                EGN = user.EGN ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                ReadingAccess = user.ReadingAccess,
                WritingAccess = user.WritingAccess,
                DepartmentDescription = user.Unit?.Department?.Description ?? string.Empty,
                UnitDescription = user.Unit?.Description ?? string.Empty,
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
                        InformationSystemDescription = ua.Access.System.Name,
                        ParentAccessDescription = ua.Access.ParentAccess?.Description ?? string.Empty,
                        Description = ua.Access.Description,
                        Directive = ua.Directive
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult EditUser(EditUserViewModel model)
        {
            var user = _userService.GetUser(model.Id);
            if (user == null) return BadRequest();

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

            _userService.SaveChanges();
            return RedirectToAction("EditUser", new { id = model.Id });
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(Guid userId, Guid unitId)
        {
            _departmentUnitService.RemoveUserUnit(userId, unitId);
            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        public IActionResult RemoveUserAccess(Guid userId, Guid accessId)
        {
            _informationSystemService.RemoveUserAccess(userId, accessId);
            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        public IActionResult UpdateUserAccessDirective(Guid userId, Guid accessId, string directive)
        {
            _informationSystemService.UpdateAccessDirective(userId, accessId, directive);
            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        public IActionResult AddUnitAccess(Guid userId, List<Guid> selectedUnitIds)
        {
            _departmentUnitService.AddUnitAccess(userId, selectedUnitIds);
            return RedirectToAction("EditUser", new { id = userId });
        }

        [HttpPost]
        public IActionResult AddAccesses(Guid userId, List<AccessViewModel> accesses)
        {
            _informationSystemService.AddAccesses(userId, accesses);
            return RedirectToAction("EditUser", new { id = userId });
        }
    }
}
