using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels.Unit;
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
            return _context.Users.IgnoreQueryFilters().Any(u => u.UserName == username && u.DeletedOn == null);
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        internal List<Department> GetAllowedDepartments(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                return _context.Units
                    .Where(u => allowedUnitIds.Contains(u.Id))
                    .Select(u => u.Department)
                    .Distinct()
                    .ToList();
            }
            else
                return _context.Departments.ToList();
        }

        internal List<Unit> GetUserAllowedUnits(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
                return _context.Units.Where(u => _context.UnitUser.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id)).ToList();
            else
                return _context.Units.ToList();
        }

        internal List<Unit> GetUserAccessibleUnits(User user, User loggedUser)
        {
            if (user.WritingAccess == AuthorityType.None || loggedUser.WritingAccess == AuthorityType.None)
                return [];

            var query = _context.Units.AsQueryable();

            if (user.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUser.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id));

            if (loggedUser.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUser.Any(uu => uu.UserId == loggedUser.Id && uu.UnitId == u.Id));

            return query.ToList();
        }

        internal List<Unit> GetUserInaccessibleUnits(User user, User loggedUser)
        {
            if (loggedUser.WritingAccess == AuthorityType.None)
                return [];

            var query = _context.Units.AsQueryable();

            if (loggedUser.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUser.Any(uu => uu.UserId == loggedUser.Id && uu.UnitId == u.Id));

            if (user.WritingAccess != AuthorityType.None)
            {
                if (user.WritingAccess == AuthorityType.Restricted)
                    query = query.Where(u => !_context.UnitUser.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id));
                else
                    return [];
            }

            return query.ToList();
        }

        internal List<Unit> GetAllowedUnitsForDepartment(User user, Guid departmentId)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                return _context.Units
                    .Where(u => allowedUnitIds.Contains(u.Id) && u.DepartmentId == departmentId)
                    .Distinct()
                    .ToList();
            }
            else
                return _context.Units.Where(u => u.DepartmentId == departmentId).ToList();
        }

        internal List<UserListItemViewModel> GetFilteredUsers(string sortBy, string filterUnit, string filterDepartment, User loggedUser)
        {
            var accessibleUnitIds = loggedUser.AccessibleUnits.Select(au => au.UnitId).ToList();
            var filteredUsers = _context.Users.Where(u => u.DeletedOn == null && accessibleUnitIds.Contains(u.UnitId));

            if (!string.IsNullOrEmpty(filterDepartment)) filteredUsers = filteredUsers.Where(u => u.Unit.Department.Description == filterDepartment);
            if (!string.IsNullOrEmpty(filterUnit)) filteredUsers = filteredUsers.Where(u => u.Unit.Description == filterUnit);

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
                    WriteAccess = u.WritingAccess,
                    ReadAccess = u.ReadingAccess
                });


            return usersQuery.ToList();
        }

        internal List<string> GetSortOptions()
        {
            return ["Достъп за писане", "Достъп за четене", "Потребителско име", "Дирекция", "Отдел"];
        }

        internal void UpdateUser(MyProfileViewModel model, User loggedUser)
        {
            UpdateUserFromModel(loggedUser, model.FirstName, model.MiddleName, model.LastName, model.EGN, model.Phone, 
                model.SelectedUnitId, model.WritingAccess, model.ReadingAccess);
            _context.SaveChanges();
        }

        internal void UpdateUser(EditUserViewModel model, User user)
        {
            UpdateUserFromModel(user, model.FirstName, model.MiddleName, model.LastName, model.EGN, model.Phone
                , model.SelectedUnitId, model.WritingAccess, model.ReadingAccess);
            _context.SaveChanges();
        }

        internal void UpdateUserFromModel(User user, string firstName, string middleName, 
            string lastName, string? egn, string? phone, Guid unitId, AuthorityType write, AuthorityType read)
        {
            if (user == null) return;

            user.FirstName = firstName;
            user.MiddleName = middleName;
            user.LastName = lastName;
            user.EGN = egn;
            user.Phone = phone;
            user.WritingAccess = write;
            user.ReadingAccess = read;

            if (unitId != user.UnitId)
            {
                user.UnitId = unitId;
                user.Unit = _context.Units.FirstOrDefault(u => u.Id == unitId) ?? throw new ArgumentException("Unit not found");
            }
        }

        internal List<User> GetAccessibleUsers(User loggedUser)
        {
            return _context.Users
                .Where(u => u.DeletedOn == null && u.Id != loggedUser.Id && loggedUser.AccessibleUnits.Any(au => au.UnitId == u.UnitId))
                .ToList();
        }

        internal void SoftDeleteUser(User userToDelete)
        {
            userToDelete.DeletedOn = DateTime.UtcNow;

            foreach (var userAccess in userToDelete.UserAccesses)
                userAccess.DeletedOn = DateTime.UtcNow;

            _context.SaveChanges();
        }

        internal void HardDeleteUser(User userToDelete)
        {
            var userAccesses = _context.UserAccesses.Where(ua => ua.UserId == userToDelete.Id);
            _context.UserAccesses.RemoveRange(userAccesses);

            var unitUsers = _context.UnitUser.Where(uu => uu.UserId == userToDelete.Id);
            _context.UnitUser.RemoveRange(unitUsers);

            _context.Users.Remove(userToDelete);
            _context.SaveChanges();
        }

        internal void HardDeleteUsers()
        {
            foreach (var user in _context.Users.IgnoreQueryFilters().Where(u => u.DeletedOn != null).ToList())
                HardDeleteUser(user);
        }

        internal List<UserListItemViewModel> GetDeletedUsers()
        {
            return _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedOn != null)
                .ToList()
                .Select(u => new UserListItemViewModel
                {
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Unit = u.Unit.Description,
                    Department = u.Unit.Department.Description,
                    WriteAccess = u.WritingAccess,
                    ReadAccess = u.ReadingAccess,
                })
                .ToList();
        }

        internal void RestoreUser(User user)
        {
            user.DeletedOn = null;

            foreach (var userAccess in user.UserAccesses)
                userAccess.DeletedOn = null;

            _context.SaveChanges();
        }

        internal void RestoreAllUsers()
        {
            foreach (var user in _context.Users.IgnoreQueryFilters().Where(u => u.DeletedOn != null).ToList())
                RestoreUser(user);
        }

        internal User? GetDeletedUser(string username)
        {
            return _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == username);
        }
    }
}
