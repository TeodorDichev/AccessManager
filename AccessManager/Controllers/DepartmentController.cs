using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class DepartmentController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly DepartmentService _departmentService;
        public DepartmentController(DepartmentService departmentUnitService, UserService userService, LogService logService)
        {
            _departmentService = departmentUnitService;
            _userService = userService;
            _logService = logService;
        }

        [HttpGet]
        public ActionResult UnitDepartmentList(string filterDepartment, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

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
                list = list.Where(d => d.DepartmentName == filterDepartment).ToList();

            int totalUsers = list.Count;

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

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            Department? dep = _departmentService.GetDepartment(id);
            if (dep == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("UnitDepartmentList");
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

            if (!ModelState.IsValid) return View(model);

            Department? dep = _departmentService.GetDepartment(model.DepartmentId.ToString());
            if (dep == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("EditDepartment", new { model.DepartmentId });
            }

            if (_departmentService.DepartmentWithDescriptionExists(model.DepartmentName))
            {
                TempData["Error"] = "Дирекция с това име вече съществува";
                return View(model);
            }

            dep.Description = model.DepartmentName;
            _userService.SaveChanges();
            _logService.AddLog(loggedUser, Data.Enums.LogAction.Edit, dep);

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
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (_departmentService.DepartmentWithDescriptionExists(DepartmentName))
            {
                TempData["Error"] = "Дирекция с това име вече същестува!";
                return RedirectToAction("UnitDepartmentList");
            }

            Department dep = _departmentService.CreateDepartment(DepartmentName);
            _logService.AddLog(loggedUser, Data.Enums.LogAction.Add, dep);

            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteDepartment(string departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Department? dep = _departmentService.GetDepartment(departmentId);
            if (dep == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("UnitDepartmentList");
            }

            _departmentService.SoftDeleteDepartment(dep);
            _logService.AddLog(loggedUser, Data.Enums.LogAction.Delete, dep);
            return RedirectToAction("UnitDepartmentList");
        }
    }
}
