using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels;
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
                && loggedUser.WritingAccess != Data.Enums.WritingAccess.Unspecified);

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
                    query = query.OrderBy(u => u.WritingAccess)
                                 .ThenBy(u => u.ReadingAccess)
                                 .ThenBy(u => u.UserName);
                    break;
                case "ReadAccess":
                    query = query.OrderBy(u => u.ReadingAccess)
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
                AvailableAccesses = context.Accesses
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Description })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

            var loggedUser = context.Users.FirstOrDefault(u => u.UserName == username);
            if (loggedUser == null) return NotFound();


            if (!ModelState.IsValid)
            {
                model.Departments = GetAllowedDepartments(loggedUser);
                model.AvailableAccesses = context.Accesses
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Description })
                    .ToList();

                if (model.SelectedDepartmentId.HasValue)
                {
                    model.Units = context.Units
                        .Where(u => u.DepartmentId == model.SelectedDepartmentId.Value)
                        .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description })
                        .ToList();
                }

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
                Password = model.Password // hash if needed
            };

            if (model.SelectedAccessibleUnitIds.Any())
            {
                user.AccessibleUnits = model.SelectedAccessibleUnitIds
                    .Select(uid => new UnitUser { UnitId = uid, UserId = user.Id })
                    .ToList();
            }

            if (model.SelectedUserAccessIds.Any())
            {
                user.UserAccesses = model.SelectedUserAccessIds
                    .Select(aid => new UserAccess { AccessId = aid, UserId = user.Id })
                    .ToList();
            }

            context.Users.Add(user);
            context.SaveChanges();

            return RedirectToAction("UserList");
        }

        [HttpGet]
        public IActionResult GetUnitsForDepartment(Guid departmentId)
        {
            var units = context.Units
                .Where(u => u.DepartmentId == departmentId)
                .Select(u => new { u.Id, u.Description })
                .ToList();

            return Json(units);
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
