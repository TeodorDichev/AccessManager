using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.Department;
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

        internal Department? GetDepartment(Guid? id)
        {
            return _context.Departments.FirstOrDefault(u => u.Id == id);
        }

        internal bool DepartmentWithNameExists(string departmentName)
        {
            return _context.Departments.Select(d => d.Description).Contains(departmentName);
        }

        internal void UpdateDepartmentName(Department department, string name)
        {
            department.Description = name;
            _context.SaveChanges();
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

        // This method expects to receive either user.WriteAuthority or user.ReadAuthority
        internal List<Department> GetDepartmentsByUserAuthority(User user, AuthorityType authority)
        {
            if (authority == AuthorityType.None)
                return [];
            else if (authority == AuthorityType.Restricted)
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

        internal Department GetDeletedDepartment(Guid departmentId)
        {
            return _context.Departments
                .IgnoreQueryFilters()
                .First(d => d.Id == departmentId && d.DeletedOn != null);
        }

        internal void RestoreDepartment(Department department)
        {
            var unitIds = department.Units.Select(u => u.Id).ToList();

            _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.Id == department.Id)
                .ExecuteUpdate(d => d.SetProperty(x => x.DeletedOn, (DateTime?)null));

            _context.Units
                .IgnoreQueryFilters()
                .Where(u => unitIds.Contains(u.Id))
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn, (DateTime?)null));
        }

        internal bool CanDeleteDepartment(Department department)
        {
            return !_context.Users.Any(u => u.Unit.DepartmentId == department.Id);
        }

        internal void SoftDeleteDepartment(Department department)
        {
            var timestamp = DateTime.Now;
            var unitIds = department.Units.Select(u => u.Id).ToList();

            _context.Departments
                .Where(d => d.Id == department.Id)
                .ExecuteUpdate(d => d.SetProperty(x => x.DeletedOn, timestamp));

            _context.UnitUsers
                .Where(u => u.Unit.DepartmentId == department.Id)
                .ExecuteDelete();

            _context.Units
                .Where(u => unitIds.Contains(u.Id))
                .ExecuteUpdate(u => u.SetProperty(x => x.DeletedOn, timestamp));
        }

        internal void HardDeleteDepartment(Department department)
        {
            var unitIds = department.Units.Select(u => u.Id).ToList();

            _context.Units
                .IgnoreQueryFilters()
                .Where(u => unitIds.Contains(u.Id))
                .ExecuteDelete();

            _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.Id == department.Id)
                .ExecuteDelete();
        }
        internal int GetDeletedUnitDepartmentsCount()
        {
            // Only accessible to admins so it does not need filtering based on user authority

            var deletedDepartmentsCount = _context.Departments
                    .IgnoreQueryFilters()
                    .Count(d => d.DeletedOn != null);

            var deletedUnitsCount = _context.Units
                .IgnoreQueryFilters()
                .Count(u => u.DeletedOn != null || u.Department.DeletedOn != null);

            return deletedDepartmentsCount + deletedUnitsCount;
        }

        internal PagedResult<UnitDepartmentViewModel> GetUnitDepartmentsPaged(User loggedUser, Department? filterDepartment, Unit? filterUnit, int page)
        {
            if (page < 1) page = 1;
            int pageSize = Constants.ItemsPerPage;

            List<UnitDepartmentViewModel> departmentItems = new();
            if (filterUnit == null)
            {
                var departmentsQuery = filterDepartment == null
                    ? GetDepartmentsByUserAuthority(loggedUser, loggedUser.ReadingAccess)
                        .OrderBy(d => d.Description).ToList()
                    : new List<Department> { filterDepartment };

                departmentItems = departmentsQuery
                    .Select(d => new UnitDepartmentViewModel
                    {
                        DepartmentId = d.Id,
                        DepartmentName = d.Description,
                        UnitName = "-"
                    })
                    .ToList();
            }

            IQueryable<Unit> unitsQuery = _context.Units;

            if (filterDepartment != null)
                unitsQuery = unitsQuery.Where(u => u.Department.Id == filterDepartment.Id);

            if (filterUnit != null)
                unitsQuery = unitsQuery.Where(u => u.Description == filterUnit.Description);

            var unitItems = unitsQuery
                .OrderBy(u => u.Description)
                .Select(u => new UnitDepartmentViewModel
                {
                    DepartmentId = u.Department.Id,
                    DepartmentName = u.Department.Description,
                    UnitName = u.Description,
                    UnitId = u.Id
                })
                .ToList();
            var allItems = departmentItems.Concat(unitItems).ToList();

            int totalCount = allItems.Count;

            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<UnitDepartmentViewModel>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = page
            };
        }


        internal PagedResult<UnitDepartmentViewModel> GetDeletedUnitDepartmentsPaged(int page)
        {
            if (page < 1) page = 1;
            int pageSize = Constants.ItemsPerPage;

            var deletedDepartmentsQuery = _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.DeletedOn != null)
                .OrderByDescending(d => d.DeletedOn);

            int deletedDepartmentsCount = deletedDepartmentsQuery.Count();
            int startRow = (page - 1) * pageSize;

            int deptRows = Math.Max(0, Math.Min(deletedDepartmentsCount - startRow, pageSize));
            var departments = Enumerable.Empty<UnitDepartmentViewModel>();
            if (deptRows > 0)
                departments = deletedDepartmentsQuery
                    .Skip(startRow)
                    .Take(deptRows)
                    .Select(d => new UnitDepartmentViewModel
                    {
                        DepartmentId = d.Id,
                        DepartmentName = d.Description,
                        UnitName = "-"
                    });

            int unitRows = pageSize - deptRows;
            var units = Enumerable.Empty<UnitDepartmentViewModel>();
            if (unitRows > 0)
            {
                var remainingDepartments = deletedDepartmentsQuery
                    .Skip(startRow + deptRows)
                    .Take(int.MaxValue)
                    .Select(d => d.Id)
                    .ToList();

                units = _context.Units
                    .IgnoreQueryFilters()
                    .Where(u => u.DeletedOn != null)
                    .Take(unitRows)
                    .Select(u => new UnitDepartmentViewModel
                    {
                        DepartmentId = u.Department.Id,
                        DepartmentName = u.Department.Description,
                        UnitName = u.Description,
                        UnitId = u.Id
                    });
            }

            var result = departments.Concat(units).ToList();
            return new PagedResult<UnitDepartmentViewModel>
            {
                Items = departments.Concat(units).ToList(),
                TotalCount = result.Count,
                Page = page
            };
        }
    }
}
