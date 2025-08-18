using AccessManager.Data;
using AccessManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
            dep.DeletedOn = DateTime.Now;
            foreach (var unit in dep.Units)
            {
                foreach (var unitUser in _context.UnitUser.Where(uu => uu.UnitId == unit.Id))
                    unitUser.DeletedOn = DateTime.Now;

                foreach (var user in unit.UsersFromUnit)
                    user.DeletedOn = DateTime.Now;

                unit.DeletedOn = DateTime.Now;
			}
            _context.SaveChanges();
        }

        internal void HardDeleteDepartment(Department dep)
        {
			foreach (var unit in dep.Units)
            {
				var unitUsers = _context.UnitUser.IgnoreQueryFilters().Where(uu => uu.UnitId == unit.Id);
				_context.UnitUser.RemoveRange(unitUsers);

				var users = _context.Users.IgnoreQueryFilters().Where(uu => uu.UnitId == unit.Id);
				_context.Users.RemoveRange(users);

                _context.Units.Remove(unit);
			}

            _context.Departments.Remove(dep);
			_context.SaveChanges();
        }
    }
}
