using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
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


        internal UnitUser? GetUnitUser(Guid userId, Guid unitId)
        {
            return _context.UnitUsers.FirstOrDefault(u => u.UserId == userId && u.UnitId == unitId);
        }

        internal UnitUser AddUnitAccess(Guid userId, Guid unitId)
        {
            UnitUser uu = new UnitUser() { UserId = userId, UnitId = unitId };
            _context.UnitUsers.Add(uu);
            _context.SaveChanges();
            return uu;
        }

        internal void AddFullUnitAccess(Guid userId)
        {
            foreach (var unit in _context.Units)
                AddUnitAccess(userId, unit.Id);
        }

        internal Unit? GetUnit(string id)
        {
            return _context.Units.FirstOrDefault(u => u.Id == Guid.Parse(id));
        }
        internal Unit? GetDeletedUnit(Guid id)
        {
            return _context.Units.IgnoreQueryFilters().FirstOrDefault(u => u.Id == id && u.DeletedOn != null);
        }

        internal Unit CreateUnit(string unitName, Guid departmentId)
        {
            Unit unit = new Unit
            {
                Description = unitName,
                DepartmentId = departmentId,
            };

            _context.Units.Add(unit);

            var users = _context.Users
                .Where(u => u.WritingAccess >= Data.Enums.AuthorityType.Restricted &&
                       u.ReadingAccess >= Data.Enums.AuthorityType.Restricted &&
                       u.Unit.DepartmentId == departmentId
                       || u.ReadingAccess >= Data.Enums.AuthorityType.Full)
                .ToList();

            foreach (var user in users)
                _context.UnitUsers.Add(new UnitUser { UserId = user.Id, UnitId = unit.Id });

            _context.SaveChanges();
            return unit;
        }
        internal List<Unit> GetUserAllowedUnits(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
                return _context.Units.Where(u => _context.UnitUsers.Any(uu => uu.UserId == user.Id && uu.UnitId == u.Id)).ToList();
            else
                return _context.Units.ToList();
        }

        internal List<Unit> GetUserAccessibleUnits(User user, User loggedUser)
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

        internal List<Unit> GetUserInaccessibleUnits(User user, User loggedUser)
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
    }
}
