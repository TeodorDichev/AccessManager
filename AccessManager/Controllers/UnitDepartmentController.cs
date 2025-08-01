using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class UnitDepartmentController : BaseController
    {
        private readonly DepartmentUnitService _departmentUnitService;
        public UnitDepartmentController(DepartmentUnitService departmentUnitService)
        {
            _departmentUnitService = departmentUnitService;
        }
    }
}
