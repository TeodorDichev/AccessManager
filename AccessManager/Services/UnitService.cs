using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Services
{
    public class UnitService
    {
        private readonly Context _context;
        public UnitService(Context context)
        {
            _context = context;
        }

        internal Unit? GetUnit(Guid? id)
        {
            return _context.Units.FirstOrDefault(u => u.Id == id);
        }

        internal Unit? GetDeletedUnit(Guid? id)
        {
            return _context.Units.IgnoreQueryFilters().FirstOrDefault(u => u.Id == id && u.DeletedOn != null);
        }

        internal List<Unit> GetUserUnits(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
                return _context.Units.Where(u => _context.UnitUsers.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id)).ToList();
            else
                return _context.Units.ToList();
        }

        internal List<Unit> GetMutualUserUnits(User user, User loggedUser)
        {
            if (user.WritingAccess == AuthorityType.None || loggedUser.WritingAccess == AuthorityType.None)
                return [];

            var query = _context.Units.AsQueryable();

            if (user.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUsers.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id));

            if (loggedUser.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUsers.Any(uu => uu.UserId == loggedUser.Id && uu.UnitId == u.Id));

            return query.ToList();
        }

        internal List<Unit> GetMutualInaccessibleUserUnits(User user, User loggedUser)
        {
            if (loggedUser.WritingAccess == AuthorityType.None)
                return [];

            var query = _context.Units.AsQueryable();

            if (loggedUser.WritingAccess == AuthorityType.Restricted)
                query = query.Where(u => _context.UnitUsers.Any(uu => uu.UserId == loggedUser.Id && uu.UnitId == u.Id));

            if (user.WritingAccess != AuthorityType.None)
            {
                if (user.WritingAccess == AuthorityType.Restricted)
                    query = query.Where(u => !_context.UnitUsers.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id));
                else
                    return [];
            }

            return query.ToList();
        }

        internal int GetUserUnitsForDepartmentCount(User user, Guid departmentId)
        {
            if (user.WritingAccess == AuthorityType.None)
                return 0;
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                return _context.Units
                    .Where(u => allowedUnitIds.Contains(u.Id) && u.DepartmentId == departmentId)
                    .Distinct()
                    .Count();
            }
            else
                return _context.Units.Where(u => u.DepartmentId == departmentId).Count();
        }

        internal List<Unit> GetUserUnitsForDepartment(User user, Department department)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];

            IQueryable<Unit> query;

            if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                query = _context.Units.Where(u => allowedUnitIds.Contains(u.Id) && u.DepartmentId == department.Id).Distinct();
            }
            else
                query = _context.Units.Where(u => u.DepartmentId == department.Id);

            return query.ToList();
        }

        internal PagedResult<UnitViewModel> GetUserUnitsForDepartmentPaged(User user, Department department, int page)
        {
            if (page < 1) page = 1;
            int pageSize = Constants.ItemsPerPage;

            IQueryable<Unit> query = _context.Units.Where(u => u.DepartmentId == department.Id);

            if (user.WritingAccess == AuthorityType.None)
            {
                return new PagedResult<UnitViewModel>
                {
                    Items = new List<UnitViewModel>(),
                    TotalCount = 0,
                    Page = page
                };
            }
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                query = query.Where(u => allowedUnitIds.Contains(u.Id));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(u => u.Description)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UnitViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Description
                })
                .ToList();

            return new PagedResult<UnitViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page
            };
        }

        internal void UpdateUnitName(Unit unit, string name)
        {
            unit.Description = name;
            _context.SaveChanges();
        }

        internal Unit CreateUnit(string unitName, Department department)
        {
            Unit unit = new Unit
            {
                Description = unitName,
                DepartmentId = department.Id,
                Department = department,
            };

            _context.Units.Add(unit);

            var users = _context.Users
                .Where(u => u.ReadingAccess >= AuthorityType.Full || u.WritingAccess >= AuthorityType.Full)
                .ToList();

            var unitUsers = users.Select(u => new UnitUser
            {
                UserId = u.Id,
                UnitId = unit.Id,
                User = u,       
                Unit = unit     
            }).ToList();

            _context.UnitUsers.AddRange(unitUsers);
            _context.SaveChanges();

            return unit;
        }

        internal UnitUser? GetUnitUser(Guid userId, Guid unitId)
        {
            return _context.UnitUsers.FirstOrDefault(u => u.UserId == userId && u.UnitId == unitId);
        }

        internal UnitUser AddUnitUser(User user, Unit unit)
        {
            UnitUser uu = new UnitUser() 
            { 
                UserId = user.Id,
                User = user,
                UnitId = unit.Id,
                Unit = unit
            };

            _context.UnitUsers.Add(uu);
            _context.SaveChanges();
            return uu;
        }

        internal void AddAllUnitUsers(User user)
        {
            foreach (var unit in _context.Units)
                AddUnitUser(user, unit);
        }

        internal bool CanDeleteUnit(Unit unit)
        {
            return !unit.UsersFromUnit.Any();
        }
        internal bool CanRestoreUnit(Unit unit)
        {
            // Needed due to dependency to department
            return _context.Departments.IgnoreQueryFilters().Any(d => d.Id == unit.DepartmentId);
        }

        internal void RestoreUnit(Unit unit)
        {
            // If deleted unit department exists it maps to it
            // If it is also deleted it restores and maps to it
            // If no then we must not restore which will be checked beforehand
            _context.Departments.IgnoreQueryFilters()
                .Where(d => d.Id == unit.DepartmentId)
                .ExecuteUpdate(d => d.SetProperty(d => d.DeletedOn, (DateTime?)null));

            _context.Units.IgnoreQueryFilters()
                .Where(u => u.Id == unit.Id)
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn, (DateTime?)null));
        }

        internal void SoftDeleteUnit(Unit unit)
        {
            var timestamp = DateTime.Now;

            _context.Units
                .Where(u => u.Id == unit.Id)
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn, timestamp));

            _context.UnitUsers
                .Where(u => u.UnitId == unit.Id)
                .ExecuteDelete();
        }

        internal void HardDeleteUnit(Unit unit)
        {
            _context.Units
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == unit.Id)
                    .ExecuteDelete();
        }

        internal void HardDeleteUnitUser(UnitUser unitUser)
        {
            _context.UnitUsers
                .Where(u => u.UserId == unitUser.UserId && u.UnitId == unitUser.UnitId)
                .ExecuteDelete();

            _context.SaveChanges();
        }

        internal PagedResult<UnitDepartmentViewModel> GetInaccessibleUnitsPaged(User loggedUser, User user, Department? filterDepartment, int page)
        {
            var inaccessibleUnitsQuery = GetMutualInaccessibleUserUnits(user, loggedUser);
            var totalInaccessible = inaccessibleUnitsQuery.Count();

            var inaccessibleUnits = inaccessibleUnitsQuery
                .OrderBy(u => u.Description)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(u => new UnitDepartmentViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Description,
                    DepartmentName = u.Department.Description,
                    DepartmentId = u.Department.Id
                })
                .ToList();

            return new PagedResult<UnitDepartmentViewModel>
            {
                Items = filterDepartment == null ? inaccessibleUnits : inaccessibleUnits.Where(u => u.DepartmentId == filterDepartment.Id).ToList(),
                TotalCount = totalInaccessible,
                Page = page
            };
        }

        internal PagedResult<UnitDepartmentViewModel> GetAccessibleUnitsPaged(User loggedUser, User user, Department? filterDepartment, int page)
        {
            var accessibleUnitsQuery = GetMutualUserUnits(user, loggedUser);

            var totalAccessible = accessibleUnitsQuery.Count();

            var accessibleUnits = accessibleUnitsQuery
                .OrderBy(u => u.Description)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(u => new UnitDepartmentViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Description,
                    DepartmentName = u.Department.Description,
                    DepartmentId = u.Department.Id
                })
                .ToList();

            return new PagedResult<UnitDepartmentViewModel>
            {
                Items = filterDepartment == null ? accessibleUnits : accessibleUnits.Where(u => u.DepartmentId == filterDepartment.Id).ToList(),
                TotalCount = totalAccessible,
                Page = page
            };

        }
    }
}
