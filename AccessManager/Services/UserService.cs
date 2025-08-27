using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.User;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AccessManager.Services
{
    public class UserService
    {
        private readonly Context _context;
        public UserService(Context context)
        {
            _context = context;
        }

        public User? GetUser(string? username)
        {
            return _context.Users.FirstOrDefault(u => u.UserName == username);
        }

        public User? GetUser(Guid? id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        public bool UserWithUsernameExists(string username)
        {
            return _context.Users.IgnoreQueryFilters().Any(u => u.UserName == username) || string.IsNullOrEmpty(username);
        }

        private IQueryable<User> ApplySorting(IQueryable<User> query, UserSortOptions sortOption)
        {
            return sortOption switch
            {
                UserSortOptions.Username => query.OrderBy(u => u.UserName),
                UserSortOptions.FirstName => query.OrderBy(u => u.FirstName),
                UserSortOptions.LastName => query.OrderBy(u => u.LastName),
                UserSortOptions.ReadingAccess => query.OrderByDescending(u => u.ReadingAccess),
                UserSortOptions.WritingAccess => query.OrderByDescending(u => u.WritingAccess),
                _ => query.OrderBy(u => u.UserName),
            };
        }

        internal PagedResult<UserListItemViewModel> GetAccessibleUsersPaged(User loggedUser, Unit? filterUnit, Department? filterDepartment, int page, UserSortOptions sortOption)
        {
            var accessibleUnitIds = _context.UnitUsers
                .Where(uu => uu.UserId == loggedUser.Id)
                .Select(uu => uu.UnitId);

            var query = _context.Users.Where(u => accessibleUnitIds.Contains(u.UnitId));

            if(filterUnit != null)
                query = query.Where(u => u.UnitId == filterUnit.Id);
            else if (filterDepartment != null)
                query = query.Where(u => u.Unit.DepartmentId == filterDepartment.Id);

            query = ApplySorting(query, sortOption);

            return new PagedResult<UserListItemViewModel>
            {
                Items = query
                    .Skip((page - 1) * Utills.Constants.ItemsPerPage)
                    .Take(Utills.Constants.ItemsPerPage)
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
                    .ToList(),
                TotalCount = query.Count(),
                Page = page
            };
        }

        internal List<User> GetAccessibleUsers(User loggedUser)
        {
            var accessibleUnitIds = _context.UnitUsers
                .Where(uu => uu.UserId == loggedUser.Id)
                .Select(uu => uu.UnitId);

            var query = _context.Users
                .Where(u => accessibleUnitIds.Contains(u.UnitId));

            return query.ToList();
        }

        internal List<User> GetAccessibleUsers(User loggedUser, Department department, int page, UserSortOptions sortOption)
        {
            var accessibleUnitIds = _context.UnitUsers
                .Where(uu => uu.UserId == loggedUser.Id)
                .Select(uu => uu.UnitId);

            var query = _context.Users
                .Where(u => accessibleUnitIds.Contains(u.UnitId) &&
                            u.Unit.DepartmentId == department.Id);

            query = ApplySorting(query, sortOption);

            return query
                .Skip((page - 1) * Utills.Constants.ItemsPerPage)
                .Take(Utills.Constants.ItemsPerPage)
                .ToList();
        }

        internal List<User> GetAccessibleUsers(User loggedUser, Unit unit, int page, UserSortOptions sortOption)
        {
            var accessibleUnitIds = _context.UnitUsers
                .Where(uu => uu.UserId == loggedUser.Id)
                .Select(uu => uu.UnitId);

            var query = _context.Users
                .Where(u => accessibleUnitIds.Contains(u.UnitId) &&
                            u.UnitId == unit.Id);

            query = ApplySorting(query, sortOption);

            return query
                .Skip((page - 1) * Utills.Constants.ItemsPerPage)
                .Take(Utills.Constants.ItemsPerPage)
                .ToList();
        }

        internal List<User> GetUsers(User loggedUser)
        {
            var accessibleUnitIds = loggedUser.AccessibleUnits.Select(au => au.UnitId).ToList();

            return _context.Users.Where(u => accessibleUnitIds.Contains(u.UnitId)).ToList();
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
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

        private void UpdateUserFromModel(User user, string firstName, string middleName,
            string lastName, string? egn, string? phone, Guid? unitId, AuthorityType write, AuthorityType read)
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
                var unit = _context.Units.FirstOrDefault(u => u.Id == unitId);
                if(unit != null) user.UnitId = unit.Id;
                   
            }
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

        internal List<User> GetDeletedUsers()
        {
            return _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedOn != null)
                .ToList();
        }

        internal User? GetDeletedUser(string username)
        {
            return _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == username && u.DeletedOn != null);
        }

        internal User? GetDeletedUser(Guid id)
        {
            return _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == id && u.DeletedOn != null);
        }

        internal PagedResult<UserListItemViewModel> GetDeletedUsersPaged(int page)
        {
            if (page < 1) page = 1;
            int pageSize = Constants.ItemsPerPage;

            var deletedUsers = GetDeletedUsers();

            var items = deletedUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListItemViewModel
                {
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Unit = u.Unit.Description,
                    Department = u.Unit.Department.Description,
                    WriteAccess = u.WritingAccess,
                    ReadAccess = u.ReadingAccess
                })
                .ToList();

            return new PagedResult<UserListItemViewModel>
            {
                Items = items,
                Page = page,
                TotalCount = deletedUsers.Count()
            };
        }
    }
}
