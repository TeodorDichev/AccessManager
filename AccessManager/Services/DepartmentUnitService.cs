using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class DepartmentUnitService
    {
        private readonly Context _context;
        public DepartmentUnitService(Context context)
        {
            _context = context;
        }

        public string GetDepartmentDescription(Guid? unitId)
        {
            if (unitId == null) return string.Empty;
            var unit = _context.Units.FirstOrDefault(u => u.Id == unitId.Value);
            return unit?.Department?.Description ?? string.Empty;
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
            return _context.Units.Where(u => u.DeletedOn == null).ToList();
        }

        public List<Department> GetDepartments()
        {
            return _context.Departments.Where(d => d.DeletedOn == null).ToList();
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

        internal Department? GetDepartment(string id)
        {
            return _context.Departments.FirstOrDefault(u => u.Id == Guid.Parse(id));
        }

        internal bool DepartmentWithDescriptionExists(string departmentName)
        {
            return _context.Departments.Select(d => d.Description).Contains(departmentName);
        }
    }
}
