using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Controllers
{
    public class UserController : BaseController
    {
        private readonly Context context;
        private readonly PasswordService passwordService;
        public UserController(Context context, PasswordService passwordService)
        {
            this.context = context;
            this.passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult UserList(string sortBy = "WriteAccess", string filterUnit = "")
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = context.Users.FirstOrDefault(u => u.UserName == username);
            if (loggedUser == null) return NotFound();

            bool canAddUser = (loggedUser.WritingAccess != Data.Enums.WritingAccess.None 
                && loggedUser.WritingAccess != Data.Enums.WritingAccess.None);

            IEnumerable<User> query = [];
            
            if (loggedUser.ReadingAccess == Data.Enums.ReadingAccess.Full)
            {
                query = context.Users.ToList();
            }
            if (loggedUser.ReadingAccess == Data.Enums.ReadingAccess.Partial)
            {
                query = context.Users.Where(u => loggedUser.AccessibleUnits.Select(au => au.Unit).Contains(u.Unit));
            }

            // Sorting
            switch (sortBy)
            {
                case "WriteAccess":
                    query = query.OrderByDescending(u => u.WritingAccess)
                                 .ThenBy(u => u.ReadingAccess)
                                 .ThenBy(u => u.UserName);
                    break;
                case "ReadAccess":
                    query = query.OrderByDescending(u => u.ReadingAccess)
                                 .ThenBy(u => u.UserName);
                    break;
                case "UserName":
                    query = query.OrderBy(u => u.UserName);
                    break;
                default:
                    query = query.OrderBy(u => u.UserName);
                    break;
            }

            if (!string.IsNullOrEmpty(filterUnit))
            {
                query = query.Where(u => u.Unit.Description == filterUnit);
            }

            var users = query.Select(u => new UserListItemViewModel
            {
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Department = u.Unit.Department.Description,
                Unit = u.Unit.Description,
                ReadAccess = AccessLocalization.GetBulgarianReadingAccess(u.ReadingAccess),
                WriteAccess = AccessLocalization.GetBulgarianWritingAccess(u.WritingAccess),
            }).ToList();

            var vm = new UserListViewModel
            {
                Users = users,
                SortOptions = new List<string> { "WriteAccess", "ReadAccess", "UserName" },
                SelectedSortOption = sortBy,
                FilterUnits = context.Units.Select(x => x.Description).ToList(),
                FilterDepartments = context.Departments.Select(x => x.Description).ToList(),
                SelectedFilterUnit = filterUnit,
                SelectedFilterDepartment = filterUnit,
                CanAddUser = canAddUser
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = context.Users.FirstOrDefault(u => u.UserName == username);
            if (loggedUser == null) return NotFound();

            var viewModel = new CreateUserViewModel
            {
                Departments = GetAllowedDepartments(loggedUser),
                CanAddToAllDepartments = loggedUser.WritingAccess == WritingAccess.Full,
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model, string? SelectedAccessibleUnitIds)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = context.Users.FirstOrDefault(u => u.UserName == username);

            if (loggedUser == null) return NotFound();

            if(context.Users.Any(u => u.UserName == model.UserName))
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
                Password = model.Password
            };

            if (!string.IsNullOrEmpty(SelectedAccessibleUnitIds))
            {
                user.AccessibleUnits = SelectedAccessibleUnitIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(uid => new UnitUser { UnitId = Guid.Parse(uid), UserId = user.Id })
                    .ToList();
            }

            context.Users.Add(user);
            context.SaveChanges();

            return RedirectToAction("MapUserToInformationSystems", new { userName = model.UserName });
        }

        [HttpGet]
        public IActionResult MapUserToInformationSystems(string userName)
        {
            var user = context.Users.FirstOrDefault(u => u.UserName == userName);
            if (user == null) return NotFound();

            var systems = context.InformationSystems
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
            var user = context.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (user == null) return NotFound();

            foreach (var system in model.Systems.Where(s => s.IsSelected))
            {
                foreach (var access in system.Accesses.Where(a => a.IsSelected))
                {
                    context.UserAccesses.Add(new UserAccess
                    {
                        UserId = user.Id,
                        AccessId = access.Id,
                        Directive = access.Directive ?? system.Directive ?? string.Empty,
                        GrantedOn = DateTime.UtcNow
                    });

                    foreach (var sub in access.SubAccesses.Where(s => s.IsSelected))
                    {
                        context.UserAccesses.Add(new UserAccess
                        {
                            UserId = user.Id,
                            AccessId = sub.Id,
                            Directive = sub.Directive ?? access.Directive ?? system.Directive ?? string.Empty,
                            GrantedOn = DateTime.UtcNow
                        });
                    }
                }
            }

            context.SaveChanges();
            return RedirectToAction("UserList");
        }

        private List<SelectListItem> GetAllowedDepartments(User user)
        {
            if (user.WritingAccess == WritingAccess.Full)
            {
                return context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                    .ToList();
            }

            var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
            return context.Units
                .Where(u => allowedUnitIds.Contains(u.Id))
                .Select(u => u.Department)
                .Distinct()
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                .ToList();
        }

    }
}
