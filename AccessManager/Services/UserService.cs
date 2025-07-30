using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.ViewModels.User;

namespace AccessManager.Services
{
    public class UserService
    {
        internal List<string> GetDepartments(User loggedUser)
        {
            return loggedUser.AccessibleUnits.Select(u => u.Unit.Department.Description).Distinct().ToList();
        }

        internal List<UserListItemViewModel> GetFilteredUsers(Context context, string sortBy, string filterUnit, string filterDepartment, User loggedUser)
        {
            var accessibleUnitIds = loggedUser.AccessibleUnits.Select(au => au.UnitId).ToList();
            var filteredUsers = context.Users.Where(u => u.DeletedOn == null && accessibleUnitIds.Contains(u.UnitId));

            if (!string.IsNullOrEmpty(filterUnit))
            {
                filteredUsers = filteredUsers.Where(u => u.Unit.Description == filterUnit);
            }

            if (!string.IsNullOrEmpty(filterDepartment))
            {
                filteredUsers = filteredUsers.Where(u => u.Unit.Department.Description == filterDepartment);
            }

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

        internal List<string> GetUnits(User loggedUser)
        {
            return loggedUser.AccessibleUnits.Select(u => u.Unit.Description).Distinct().ToList();
        }
    }
}
