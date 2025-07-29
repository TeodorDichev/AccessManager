using AccessManager.Data;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class UnitDepartmentController : BaseController
    {
        private readonly Context context;

        public UnitDepartmentController(Context context)
        {
            this.context = context;
        }

        [HttpGet]
        public IActionResult GetUnitsForDepartment(Guid departmentId)
        {
            var units = context.Units
                .Where(u => u.DepartmentId == departmentId)
                .Select(u => new { u.Id, u.Description })
                .ToList();

            return Json(units);
        }

        [HttpGet]
        public IActionResult GetDepartmentUnits()
        {
            var departments = context.Departments
                .Select(d => new
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Description,
                    Units = d.Units.Select(u => new
                    {
                        UnitId = u.Id,
                        UnitName = u.Description
                    })
                })
                .ToList();

            return Json(departments);
        }
    }
}
