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

        internal bool CanDeleteUser(User user)
        {
            return !_context.UserAccesses.Any(ua => ua.UserId == user.Id)
                && !_context.UnitUsers.Any(uu => uu.UserId == user.Id);
        }

        internal void RestoreUser(User user)
        {
            _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == user.Id)
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn,(DateTime?)null));
        }

        internal void SoftDeleteUser(User user)
        {
            var timestamp = DateTime.Now;

            _context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn, timestamp));
        }

        internal void HardDeleteUser(User user)
        {
            _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == user.Id)
                .ExecuteDelete();
        }

        internal void HardDeleteUsers()
        {
            _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedOn != null)
                .ExecuteDelete();
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

        internal User? GetDeletedUser(string username)
        {
            return _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == username);
        }
    }
}
