using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly Context _context;
        private readonly PasswordService _passwordService;
        private readonly UserService _userService;
        public UserController(Context context, PasswordService passwordService, UserService userService)
        {
            _context = context;
            _passwordService = passwordService;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult UserList(string sortBy, string filterUnit, string filterDepartment, int page = 1)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = _context.Users.FirstOrDefault(u => u.UserName == username);
            if (loggedUser == null) return NotFound();

            int pageSize = 20;

            List<UserListItemViewModel> allUsers = _userService.GetFilteredUsers(_context, sortBy, filterUnit, filterDepartment, loggedUser);
            int totalUsers = allUsers.Count();

            List<UserListItemViewModel> users = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new UserListViewModel
            {
                Users = users,
                SortOptions = _userService.GetSortOptions(),
                SelectedSortOption = sortBy,
                FilterUnits = _userService.GetUnitDescriptions(loggedUser),
                SelectedFilterUnit = filterUnit,
                FilterDepartments = _userService.GetDepartmentDescriptions(loggedUser),
                SelectedFilterDepartment = filterDepartment,
                HasWriteAuthority = (loggedUser.WritingAccess != AuthorityType.None),
                IsSuperAdmin = loggedUser.WritingAccess == AuthorityType.SuperAdmin,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            CreateUserViewModel model;

            if (!string.IsNullOrEmpty(pendingUserJson))
            {
                model = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson) ?? new CreateUserViewModel();
            }
            else
            {
                model = new CreateUserViewModel();
            }

            model.Departments = _context.Departments
                    .Where(d => d.DeletedOn == null)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Description
                    })
                    .ToList();

            model.Units = _context.Units
                .Where(u => u.DeletedOn == null)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Description
                })
                .ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if(_context.Users.Any(u => u.UserName == model.UserName && u.DeletedOn == null))
            {
                ModelState.AddModelError("UserName", "Потребител с това потребителско име вече съществува.");
            }

            if (!ModelState.IsValid)
            {
                model.Departments = _context.Departments
                    .Where(d => d.DeletedOn == null)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                    .ToList();

                if (model.SelectedDepartmentId.HasValue)
                {
                    model.Units = _context.Units
                        .Where(u => u.DepartmentId == model.SelectedDepartmentId && u.DeletedOn == null)
                        .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description })
                        .ToList();
                }
                else
                {
                    model.Units = new List<SelectListItem>();
                }
                return View(model);
            }

            HttpContext.Session.SetString("PendingUser", JsonSerializer.Serialize(model));

            return RedirectToAction("MapUserToInformationSystems");
        }

        [HttpGet]
        public IActionResult MapUserToInformationSystems()
        {
            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(pendingUserJson))
            {
                return RedirectToAction("CreateUser");
            }

            var pendingUser = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson);

            var systems = _context.InformationSystems
                .Where(s => s.DeletedOn == null)
                .ToList();

            var model = new MapUserToSystemsViewModel
            {
                // Fill in Systems and also copy pendingUser fields if needed for display
                UserName = pendingUser.UserName,
                FirstName = pendingUser.FirstName,
                LastName = pendingUser.LastName,
                DepartmentDescription = GetDepartmentDescription(pendingUser.SelectedUnitId),
                UnitDescription = GetUnitDescription(pendingUser.SelectedUnitId),
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
            var json = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("CreateUser");

            var model = JsonSerializer.Deserialize<CreateUserViewModel>(json);

            model.Departments = _context.Departments
                .Where(d => d.DeletedOn == null)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                .ToList();

            // Reload Units dropdown if a department is selected
            if (model.SelectedDepartmentId.HasValue)
            {
                model.Units = _context.Units
                    .Where(u => u.DepartmentId == model.SelectedDepartmentId && u.DeletedOn == null)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description })
                    .ToList();
            }
            else
            {
                model.Units = new List<SelectListItem>();
            }

            return View("CreateUser", model);
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
            var pendingUserJson = HttpContext.Session.GetString("PendingUser");
            if (string.IsNullOrEmpty(pendingUserJson))
            {
                return RedirectToAction("CreateUser");
            }

            var pendingUser = JsonSerializer.Deserialize<CreateUserViewModel>(pendingUserJson);
            if (pendingUser == null)
            {
                return RedirectToAction("CreateUser");
            }

            var user = new User
            {
                UserName = pendingUser.UserName,
                FirstName = pendingUser.FirstName,
                MiddleName = pendingUser.MiddleName,
                LastName = pendingUser.LastName,
                EGN = pendingUser.EGN,
                Phone = pendingUser.Phone,
                UnitId = pendingUser.SelectedUnitId.Value,
                ReadingAccess = pendingUser.SelectedReadingAccess,
                WritingAccess = pendingUser.SelectedWritingAccess,
                Password = _passwordService.HashPassword(pendingUser.Password)
            };

            if (!string.IsNullOrEmpty(pendingUser.SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = pendingUser.SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            foreach (var system in model.Systems.Where(s => s.IsSelected))
            {
                foreach (var access in system.Accesses.Where(a => a.IsSelected))
                {
                    _context.UserAccesses.Add(new UserAccess
                    {
                        UserId = user.Id,
                        AccessId = access.Id,
                        Directive = access.Directive ?? system.Directive ?? string.Empty,
                        GrantedOn = DateTime.UtcNow
                    });

                    foreach (var sub in access.SubAccesses.Where(s => s.IsSelected))
                    {
                        _context.UserAccesses.Add(new UserAccess
                        {
                            UserId = user.Id,
                            AccessId = sub.Id,
                            Directive = sub.Directive ?? access.Directive ?? system.Directive ?? string.Empty,
                            GrantedOn = DateTime.UtcNow
                        });
                    }
                }
            }

            HttpContext.Session.Remove("PendingUser");
            _context.SaveChanges();

            return RedirectToAction("UserList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest();
            }

            var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.DeletedOn == null);
            if (user == null)
            {
                return NotFound();
            }

            var loggedUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = _context.Users.FirstOrDefault(u => u.UserName == loggedUsername);

            if (loggedUser == null) return NotFound();

            user.DeletedOn = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDeleteUsers()
        {
            var softDeletedUsers = _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedOn != null)
                .ToList();

            if (softDeletedUsers.Any())
            {
                var userIds = softDeletedUsers.Select(u => u.Id).ToList();

                var unitUsers = _context.UnitUser
                    .Where(uu => userIds.Contains(uu.UserId));
                _context.UnitUser.RemoveRange(unitUsers);

                var userAccesses = _context.UserAccesses
                    .Where(ua => userIds.Contains(ua.UserId));
                _context.UserAccesses.RemoveRange(userAccesses);

                _context.Users.RemoveRange(softDeletedUsers);

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UserList");
        }
        private string GetDepartmentDescription(Guid? unitId)
        {
            if (unitId == null) return string.Empty;

            var unit = _context.Units
                .Include(u => u.Department)
                .FirstOrDefault(u => u.Id == unitId.Value);

            return unit?.Department?.Description ?? string.Empty;
        }

        private string GetUnitDescription(Guid? unitId)
        {
            if (unitId == null) return string.Empty;

            var unit = _context.Units.FirstOrDefault(u => u.Id == unitId.Value);

            return unit?.Description ?? string.Empty;
        }

        private void PopulateDepartmentsAndUnits(Guid? selectedDepartmentId, Guid? selectedUnitId)
        {
            var departments = _context.Departments
                .Where(d => d.DeletedOn == null)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Description
                }).ToList();

            ViewBag.Departments = departments;

            var units = _context.Units
                .Where(u => u.DeletedOn == null && u.DepartmentId == selectedDepartmentId)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Description
                }).ToList();

            ViewBag.Units = units;
        }

    }
}
