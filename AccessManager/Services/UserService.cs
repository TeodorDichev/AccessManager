using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.ViewModels.User;
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
            var accessibleUnitIds = loggedUser.AccessibleUnits.Select(au => au.UnitId).ToList();

            return _context.Users
                .Where(u => u.Id != loggedUser.Id && accessibleUnitIds.Contains(u.UnitId))
                .ToList();
        }

        internal void SoftDeleteUser(User userToDelete)
        {
            userToDelete.DeletedOn = DateTime.Now;

            foreach (var userAccess in userToDelete.UserAccesses)
                userAccess.DeletedOn = DateTime.Now;

            foreach (var unitUser in userToDelete.AccessibleUnits)
                unitUser.DeletedOn = DateTime.Now;

            _context.SaveChanges();
        }

        internal void HardDeleteUser(User userToDelete)
        {
            var userAccesses = _context.UserAccesses.IgnoreQueryFilters().Where(ua => ua.UserId == userToDelete.Id);
            _context.UserAccesses.RemoveRange(userAccesses);

            var unitUsers = _context.UnitUser.IgnoreQueryFilters().Where(uu => uu.UserId == userToDelete.Id);
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
                if(userAccess.Access != null)
                    userAccess.DeletedOn = null;

            foreach (var unitUser in user.AccessibleUnits)
                if(unitUser.Unit != null)
                    unitUser.DeletedOn = null;

            _context.SaveChanges();
        }

        internal User? GetDeletedUser(string username)
        {
            return _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == username);
        }
    }
}
