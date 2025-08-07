using AccessManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class UnitDepartmentController : BaseController
    {
        private readonly DepartmentUnitService _departmentUnitService;
        private readonly UserService _userService;
        public UnitDepartmentController(DepartmentUnitService departmentUnitService, UserService userService)
        {
            _departmentUnitService = departmentUnitService;
            _userService = userService;
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(string username, Guid unitId)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();

            _departmentUnitService.RemoveUserUnit(user.Id, unitId);
            return RedirectToAction("EditUser", new { UserName = username });
        }

        [HttpGet]
        public IActionResult GetAccessibleUnitsForUserDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            var res = loggedUser.AccessibleUnits.Where(au => au.Unit.DepartmentId == departmentId).Select(au => new { au.UnitId, au.Unit.Description });
            return Json(res);
        }

        [HttpGet]
        public IActionResult GetAccessibleDepartmentsForUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login");

            var departments = loggedUser.AccessibleUnits
                .GroupBy(u => u.Unit.Department)
                .Select(g => new
                {
                    DepartmentId = g.Key.Id,
                    DepartmentName = g.Key.Description,
                    Units = g.Select(u => new
                    {
                        UnitId = u.UnitId,
                        UnitName = u.Unit.Description
                    }).ToList()
                })
                .ToList();

            return Json(departments);
        }

    }
}
