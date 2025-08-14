using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class UnitService
    {
        private readonly Context _context;
        public UnitService(Context context)
        {
            _context = context;
        }

        public string GetUnitDescription(Guid? unitId)
        {
            if (unitId == null) return string.Empty;
            var unit = _context.Units.FirstOrDefault(u => u.Id == unitId.Value);
            return unit?.Description ?? string.Empty;
        }

        public List<Unit> GetUnitsForDepartment(Guid departmentId)
        {
            return _context.Units.Where(u => u.DepartmentId == departmentId && u.DeletedOn == null).ToList();
        }

        public List<Unit> GetUnits()
        {
            return _context.Units.ToList();
        }
        internal void RemoveUserUnit(Guid userId, Guid unitId)
        {
            var uu = _context.UnitUser.FirstOrDefault(u => u.UserId == userId && u.UnitId == unitId);
            if (uu != null)
            {
                _context.UnitUser.Remove(uu);
                _context.SaveChanges();
            }
        }

        internal void AddUnitAccess(Guid userId, List<Guid> selectedUnitIds)
        {
            foreach (var unitId in selectedUnitIds)
            {
                bool exists = _context.UnitUser.Any(uu => uu.UserId == userId && uu.UnitId == unitId);
                if (!exists)
                {
                    _context.UnitUser.Add(new UnitUser { UserId = userId, UnitId = unitId });
                }
            }

            _context.SaveChanges();
        }

        internal void AddFullUnitAccess(Guid userId)
        {
            AddUnitAccess(userId, _context.Units.Select(u => u.Id).ToList());
        }

        internal Unit? GetUnit(string id)
        {
            return _context.Units.FirstOrDefault(u => u.Id == Guid.Parse(id));
        }

        internal void CreateUnit(string unitName, Guid departmentId)
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
            {
                _context.UnitUser.Add(new UnitUser { UserId = user.Id, UnitId = unit.Id });
            }

            _context.SaveChanges();
        }
        internal void SoftDeleteUnit(string unitId)
        {
            if (_context.Units.Any(d => d.Id == Guid.Parse(unitId)))
            {
                var unit = _context.Units.FirstOrDefault(d => d.Id == Guid.Parse(unitId));
                if (unit != null)
                {
                    unit.DeletedOn = DateTime.UtcNow;
                    foreach (var unitUser in _context.UnitUser.Where(uu => uu.UnitId == unit.Id))
                    {
                        _context.UnitUser.Remove(unitUser);
                    }
                    foreach (var user in unit.UsersFromUnit)
                    {
                        user.DeletedOn = DateTime.UtcNow;
                    }
                    _context.SaveChanges();
                }
            }
        }
        internal void RemoveUnitAccess(Guid userId, List<Guid> removeIds)
        {
            foreach (var unitId in removeIds)
            {
                var uu = _context.UnitUser.FirstOrDefault(uu => uu.UserId == userId && uu.UnitId == unitId);
                if (uu != null)
                {
                    _context.UnitUser.Remove(uu);
                }
            }

            _context.SaveChanges();
        }
    }
}
