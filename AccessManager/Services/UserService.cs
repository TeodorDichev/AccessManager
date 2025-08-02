using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Services
{
    public class UserService
    {
        private readonly Context _context;
        public UserService(Context context)
        {
            _context = context;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public User? GetUser(string? username)
        {
            return _context.Users.FirstOrDefault(u => u.UserName == username && u.DeletedOn == null);
        }

        public User? GetUser(Guid? id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id && u.DeletedOn == null);
        }

        public bool UserWithUsernameExists(string username)
        {
            return _context.Users.Any(u => u.UserName == username && u.DeletedOn == null);
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        internal void SoftDeleteUser(User userToDelete)
        {
            userToDelete.DeletedOn = DateTime.UtcNow;
            _context.SaveChanges();
        }

        internal void HardDeleteUsers()
        {
            var softDeletedUsers = _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedOn != null)
                .ToList();

            if (softDeletedUsers.Count != 0)
            {
                var userIds = softDeletedUsers.Select(u => u.Id).ToList();

                var unitUsers = _context.UnitUser
                    .Where(uu => userIds.Contains(uu.UserId));
                _context.UnitUser.RemoveRange(unitUsers);

                var userAccesses = _context.UserAccesses
                    .Where(ua => userIds.Contains(ua.UserId));

                _context.UserAccesses.RemoveRange(userAccesses);
                _context.Users.RemoveRange(softDeletedUsers);
                 _context.SaveChanges();
            }
        }

        public bool canUserEditUser(User user, User other)
        {
            if (other.WritingAccess == AuthorityType.SuperAdmin || other.ReadingAccess == AuthorityType.SuperAdmin) return false;
            else if (user.WritingAccess == AuthorityType.SuperAdmin || user.WritingAccess == AuthorityType.Full) return true;
            else if (user.WritingAccess == AuthorityType.Restricted) return user.AccessibleUnits.Any(au => au.UnitId == other.UnitId);
            else return false;
        }

        internal List<SelectListItem> GetAllowedDepartmentsAsSelectListItem(User user)
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

        internal List<SelectListItem> GetAllowedUnitsAsSelectListItem(User user)
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
                    .Distinct()
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description })
                    .ToList();
            }
            else
            {
                return _context.Units
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Description })
                    .ToList();
            }
        }

        internal List<UserListItemViewModel> GetFilteredUsers(string sortBy, string filterUnit, string filterDepartment, User loggedUser)
        {
            var accessibleUnitIds = loggedUser.AccessibleUnits.Select(au => au.UnitId).ToList();
            var filteredUsers = _context.Users.Where(u => u.DeletedOn == null && accessibleUnitIds.Contains(u.UnitId));

            if (!string.IsNullOrEmpty(filterUnit)) filteredUsers = filteredUsers.Where(u => u.Unit.Description == filterUnit);
            if (!string.IsNullOrEmpty(filterDepartment)) filteredUsers = filteredUsers.Where(u => u.Unit.Department.Description == filterDepartment);

            filteredUsers = sortBy switch
            {
                "Достъп за писане" => filteredUsers.OrderByDescending(u => u.WritingAccess),
                "Достъп за четене" => filteredUsers.OrderByDescending(u => u.ReadingAccess),
                "Потребителско име" => filteredUsers.OrderBy(u => u.UserName),
                "Дирекция" => filteredUsers.OrderBy(u => u.Unit.Department.Description),
                "Отдел" => filteredUsers.OrderBy(u => u.Unit.Description),
                _ => filteredUsers.OrderBy(u => u.WritingAccess)
            };

            var usersQuery = filteredUsers
                .ToList()
                .Select(u => new UserListItemViewModel
                {
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Unit = u.Unit.Description,
                    Department = u.Unit.Department.Description,
                    WriteAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(u.WritingAccess),
                    ReadAccess = AuthorityTypeLocalization.GetBulgarianAuthorityType(u.ReadingAccess)
                });


            return usersQuery.ToList();
        }

        internal List<string> GetSortOptions()
        {
            return ["Достъп за писане", "Достъп за четене", "Потребителско име", "Дирекция", "Отдел"];
        }
    }
}
