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

        public List<Department> GetDepartments()
        {
            return _context.Departments.Where(d => d.DeletedOn == null).ToList();
        }
    }
}
