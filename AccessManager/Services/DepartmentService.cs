using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class DepartmentService
    {
        private readonly Context _context;
        public DepartmentService(Context context)
        {
            _context = context;
        }

        internal Department? GetDepartment(string id)
        {
            return _context.Departments.FirstOrDefault(u => u.Id == Guid.Parse(id));
        }

        internal bool DepartmentWithDescriptionExists(string departmentName)
        {
            return _context.Departments.Select(d => d.Description).Contains(departmentName);
        }

        internal Department CreateDepartment(string departmentName)
        {
            Department department = new Department
            {
                Description = departmentName,
            };

            _context.Departments.Add(department);
            _context.SaveChanges();

            return department;
        }

        internal void SoftDeleteDepartment(Department dep)
        {
            if (dep != null)
            {
                dep.DeletedOn = DateTime.UtcNow;
                foreach (var unit in dep.Units)
                {
                    unit.DeletedOn = DateTime.UtcNow;
                    foreach (var unitUser in _context.UnitUser.Where(uu => uu.UnitId == unit.Id))
                        _context.UnitUser.Remove(unitUser);

                    foreach (var user in unit.UsersFromUnit)
                        user.DeletedOn = DateTime.UtcNow;
                }
                _context.SaveChanges();
            }
        }
    }
}
