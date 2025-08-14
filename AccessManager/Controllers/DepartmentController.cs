using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Unit;
using AccessManager.ViewModels.UnitDepartment;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class DepartmentController : BaseController
    {
        private readonly DepartmentService _departmentService;
        private readonly UserService _userService;
        public DepartmentController(DepartmentService departmentUnitService, UserService userService)
        {
            _departmentService = departmentUnitService;
            _userService = userService;
        }

        [HttpGet]
        public ActionResult UnitDepartmentList(string filterDepartment, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");


            List<DepartmentViewModel> list = _userService.GetAllowedDepartments(loggedUser)
                .Select(g => new DepartmentViewModel
                {
                    DepartmentId = g.Id,
                    DepartmentName = g.Description,
                    Units = g.Units.Select(u => new UnitViewModel
                    {
                        UnitId = u.Id,
                        UnitName = u.Description,
                        DepartmentName = u.Department.Description,
                    }).ToList()
                })
                .ToList();

            if (!string.IsNullOrEmpty(filterDepartment))
            {
                list = list.Where(d => d.DepartmentName == filterDepartment).ToList();
            }

            int totalUsers = list.Count;
            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.Full)
            {
                ViewBag.IsReadOnly = true;
            }

            UnitDepartmentListViewModel model = new UnitDepartmentListViewModel
            {
                Departments = list,
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage),
                FilterDepartments = loggedUser.AccessibleUnits.Select(u => u.Unit.Department.Description).Distinct().ToList(),
                SelectedFilterDepartment = filterDepartment,

            };

            return View(model);
        }

        [HttpGet]
        public ActionResult EditDepartment(string id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Department? dep = _departmentService.GetDepartment(id);
            if (dep == null) return NotFound();

            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.Full)
            {
                ViewBag.IsReadOnly = true;
            }

            DepartmentEditViewModel model = new DepartmentEditViewModel
            {
                DepartmentId = dep.Id,
                DepartmentName = dep.Description,
                Units = _userService.GetAllowedUnitsForDepartment(loggedUser, Guid.Parse(id)).Select(u => new UnitListItemViewModel { UnitId = u.Id, UnitName = u.Description }).ToList(),
                WriteAuthority = loggedUser.WritingAccess
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EditDepartment(DepartmentEditViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            if (!ModelState.IsValid) return View(model);

            Department? dep = _departmentService.GetDepartment(model.DepartmentId.ToString());
            if (dep == null) return NotFound();

            if (_departmentService.DepartmentWithDescriptionExists(model.DepartmentName)) ModelState.AddModelError("", "Дирекция с това име съществува");
            dep.Description = model.DepartmentName;
            _userService.SaveChanges();

            return View(model);
        }

        [HttpGet]
        public IActionResult GetAccessibleUnitsForUserDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var res = _userService.GetAllowedUnitsForDepartment(loggedUser, departmentId).Select(u => new { UnitId = u.Id, u.Description });
            return Json(res);
        }

        [HttpGet]
        public IActionResult GetAccessibleDepartmentsForUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var departments = _userService.GetUserAllowedUnits(loggedUser)
                .GroupBy(u => u.Department)
                .Select(g => new
                {
                    DepartmentId = g.Key.Id,
                    DepartmentName = g.Key.Description,
                    Units = g.Select(u => new
                    {
                        UnitId = u.Id,
                        UnitName = u.Description
                    }).ToList()
                })
                .ToList();

            return Json(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNewDepartment(string DepartmentName)
        {
            _departmentService.CreateDepartment(DepartmentName);
            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteDepartment(string departmentId)
        {
            _departmentService.SoftDeleteDepartment(departmentId);
            return RedirectToAction("UnitDepartmentList");
        }
    }
}
