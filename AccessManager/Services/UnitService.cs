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

        internal void RemoveUserUnit(Guid userId, Guid unitId)
        {
            var uu = _context.UnitUser.FirstOrDefault(u => u.UserId == userId && u.UnitId == unitId);
            if (uu != null)
            {
                _context.UnitUser.Remove(uu);
                _context.SaveChanges();
            }
        }

        internal void AddUnitAccess(Guid userId, Guid unitId)
        {
            if (!_context.UnitUser.Any(uu => uu.UserId == userId && uu.UnitId == unitId))
            {
                _context.UnitUser.Add(new UnitUser { UserId = userId, UnitId = unitId });
                _context.SaveChanges();
            }
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
                _context.UnitUser.Add(new UnitUser { UserId = user.Id, UnitId = unit.Id });

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
                        _context.UnitUser.Remove(unitUser);

                    foreach (var user in unit.UsersFromUnit)
                        user.DeletedOn = DateTime.UtcNow;
                    _context.SaveChanges();
                }
            }
        }

        internal void RemoveUnitAccess(Guid userId, Guid unitId)
        {
            var uu = _context.UnitUser.FirstOrDefault(uu => uu.UserId == userId && uu.UnitId == unitId);
            if (uu != null)
            {
                _context.UnitUser.Remove(uu);
                _context.SaveChanges();
            }
        }
    }
}
