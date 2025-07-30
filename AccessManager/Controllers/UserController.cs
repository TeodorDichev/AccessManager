using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
                FilterUnits = _userService.GetUnits(loggedUser),
                SelectedFilterUnit = filterUnit,
                FilterDepartments = _userService.GetDepartments(loggedUser),
                SelectedFilterDepartment = filterDepartment,
                CanAddUser = (loggedUser.WritingAccess != AuthorityType.None),
                IsSuperAdmin = loggedUser.WritingAccess == AuthorityType.SuperAdmin,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
            };

            return View(model);
        }


        [HttpGet]
        public IActionResult CreateUser()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = _context.Users.FirstOrDefault(u => u.UserName == username);
            if (loggedUser == null) return NotFound();

            var viewModel = new CreateUserViewModel
            {
                Departments = GetAllowedDepartments(loggedUser),
                CanAddToAllDepartments = loggedUser.WritingAccess == AuthorityType.Full,
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model, string? SelectedAccessibleUnitIds)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = _context.Users.FirstOrDefault(u => u.UserName == username);

            if (loggedUser == null) return NotFound();

            if (_context.Users.Any(u => u.UserName == model.UserName))
            {
                ModelState.AddModelError("UserName", "Невалидно потребителско име!");
            }

            if (!ModelState.IsValid)
            {
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
                Password = _passwordService.HashPassword(model.Password)
            };

            if (!string.IsNullOrEmpty(SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("MapUserToInformationSystems", new { userName = model.UserName });
        }

        [HttpGet]
        public IActionResult MapUserToInformationSystems(string userName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
            if (user == null) return NotFound();

            var systems = _context.InformationSystems
                .Where(s => s.DeletedOn == null)
                .ToList();

            var model = new MapUserToSystemsViewModel
            {
                UserName = userName,
                Systems = systems.Select(s => new InformationSystemViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Accesses = s.Accesses
                        .Where(a => a.DeletedOn == null)
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

        [HttpPost]
        public IActionResult MapUserToInformationSystems(MapUserToSystemsViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (user == null) return NotFound();

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

            _context.SaveChanges();
            return RedirectToAction("UserList");
        }

        private List<SelectListItem> GetAllowedDepartments(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
            {
                return [];
            }
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                return _context.Units
                    .Where(u => allowedUnitIds.Contains(u.Id))
                    .Select(u => u.Department)
                    .Distinct()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                    .ToList();
            }
            else
            {
                return _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                    .ToList();
            }
        }
    }
}
