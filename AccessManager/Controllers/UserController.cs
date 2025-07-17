using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.ViewModels;
using Microsoft.AspNetCore.Mvc;

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

            var allUnits = context.Units.Select(x => x.Description).ToList();

            var vm = new UserListViewModel
            {
                Users = users,
                SortOptions = new List<string> { "WriteAccess", "ReadAccess", "UserName" },
                SelectedSortOption = sortBy,
                FilterUnits = allUnits,
                SelectedFilterUnit = filterUnit,
                CanAddUser = canAddUser
            };

            return View(vm);
        }
    }
}
