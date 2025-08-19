using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Runtime.Intrinsics.Arm;

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
        internal List<Department> GetAllowedDepartments(User user)
        {
            if (user.WritingAccess == AuthorityType.None)
                return [];
            else if (user.WritingAccess == AuthorityType.Restricted)
            {
                var allowedUnitIds = user.AccessibleUnits.Select(au => au.UnitId).ToList();
                return _context.Units
                    .Where(u => allowedUnitIds.Contains(u.Id))
                    .Select(u => u.Department)
                    .Distinct()
                    .ToList();
            }
            else
                return _context.Departments.ToList();
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

        internal int GetDeletedUnitDepartmentsCount()
        {
            // Only accessible to admins so it does not need filtering based on user authority

            return _context.Departments
                .IgnoreQueryFilters()
                .Count(d => d.DeletedOn != null)
                +
                _context.Units
                .IgnoreQueryFilters()
                .Count(u => u.Department.DeletedOn != null)
                +
                _context.Units
                .IgnoreQueryFilters()
                .Count(u => u.DeletedOn != null && u.Department.DeletedOn == null);
        }

        internal IEnumerable<UnitDepartmentViewModel> GetDeletedUnitDepartments(int page)
        {
            if (page < 1) page = 1;
            int pageSize = Constants.ItemsPerPage;

            // total deleted departments
            var deletedDepartmentsQuery = _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.DeletedOn != null)
                .OrderByDescending(d => d.DeletedOn);

            int deletedDepartmentsCount = deletedDepartmentsQuery.Count();

            int startRow = (page - 1) * pageSize;

            // Departments part
            int deptRows = Math.Max(0, Math.Min(deletedDepartmentsCount - startRow, pageSize));
            var departments = Enumerable.Empty<UnitDepartmentViewModel>();
            if (deptRows > 0)
            {
                departments = deletedDepartmentsQuery
                    .Skip(startRow)
                    .Take(deptRows)
                    .Select(d => new UnitDepartmentViewModel
                    {
                        DepartmentId = d.Id,
                        DepartmentName = d.Description,
                        UnitName = "-"
                    });
            }

            // Units part
            int unitRows = pageSize - deptRows;
            var units = Enumerable.Empty<UnitDepartmentViewModel>();
            if (unitRows > 0)
            {
                // get departments not already included
                var remainingDepartments = deletedDepartmentsQuery
                    .Skip(startRow + deptRows)
                    .Take(int.MaxValue) // all remaining deleted departments
                    .Select(d => d.Id)
                    .ToList();

                units = _context.Units
                    .IgnoreQueryFilters()
                    .Where(u => u.DeletedOn != null || u.Department.DeletedOn != null)
                    .Where(u => remainingDepartments.Contains(u.Department.Id))
                    .OrderByDescending(u => u.DeletedOn ?? u.Department.DeletedOn)
                    .Take(unitRows)
                    .Select(u => new UnitDepartmentViewModel
                    {
                        DepartmentId = u.Department.Id,
                        DepartmentName = u.Department.Description,
                        UnitName = u.Description
                    });
            }

            return departments.Concat(units).ToList();
        }

        internal Department GetDeletedDepartment(Guid departmentId)
        {
            return _context.Departments
                .IgnoreQueryFilters()
                .First(d => d.Id == departmentId && d.DeletedOn != null);
        }

        internal void RestoreDepartment(Department department)
        {
            department.DeletedOn = null;
            foreach (var unit in department.Units)
            {
                foreach (var unitUser in _context.UnitUser.Where(uu => uu.UnitId == unit.Id))
                    unitUser.DeletedOn = null;

                foreach (var user in unit.UsersFromUnit)
                    user.DeletedOn = null;

                unit.DeletedOn = null;
            }
            _context.SaveChanges();
        }
    }
}
