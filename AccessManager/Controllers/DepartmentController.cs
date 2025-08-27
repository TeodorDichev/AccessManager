using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels.Department;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.Controllers
{
    public class DepartmentController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly UnitService _unitService;
        private readonly DepartmentService _departmentService;
        public DepartmentController(DepartmentService departmentUnitService, UserService userService, LogService logService, UnitService unitService)
        {
            _departmentService = departmentUnitService;
            _userService = userService;
            _logService = logService;
            _unitService = unitService;
        }

        [HttpGet]
        public IActionResult SearchDepartments(string term)
       {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var results = _departmentService.GetDepartmentsByUserAuthority(loggedUser, loggedUser.WritingAccess)
                .Where(d => string.IsNullOrEmpty(term) || d.Description.Contains(term))
                .Select(d => new { id = d.Id, text = d.Description })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public ActionResult UnitDepartmentList(Guid? filterDepartmentId, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var dep = _departmentService.GetDepartment(filterDepartmentId);

            UnitDepartmentListViewModel model = new UnitDepartmentListViewModel
            {
                UnitDepartments = _departmentService.GetUnitDepartmentsPaged(loggedUser, dep, page),
                WriteAuthority = loggedUser.WritingAccess,
                FilterDepartmentId = filterDepartmentId,
                FilterDepartmentDescription = dep?.Description ?? "",
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult EditDepartment(Guid? id, int page = 1)
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
                Units = _unitService.GetUserUnitsForDepartmentPaged(loggedUser, dep, page),
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

            Department? dep = _departmentService.GetDepartment(model.DepartmentId);
            if (dep == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("EditDepartment", new { model.DepartmentId });
            }

            if (_departmentService.DepartmentWithNameExists(model.DepartmentName))
            {
                TempData["Error"] = "Дирекция с това име вече съществува";
                return View(model);
            }

            _departmentService.UpdateDepartmentName(dep, model.DepartmentName);
            _logService.AddLog(loggedUser, Data.Enums.LogAction.Edit, dep);

            return View(model);
        }

        [HttpGet]
        public IActionResult GetAccessibleDepartmentsForUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var departments = _unitService.GetUserUnits(loggedUser)
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

            if (_departmentService.DepartmentWithNameExists(DepartmentName))
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
        public IActionResult SoftDeleteDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Department? dep = _departmentService.GetDepartment(departmentId);
            if (dep == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("UnitDepartmentList");
            }
            else if (!_departmentService.CanDeleteDepartment(dep))
            {
                TempData["Error"] = "Дирекцията не може да бъде изтрит понеже тя или някои от отделите и са свързани с потребители!";
                return RedirectToAction("UnitDepartmentList");
            }

            _logService.AddLog(loggedUser, LogAction.Delete, dep);
            _departmentService.SoftDeleteDepartment(dep);
            TempData["Success"] = "Дирекцията е успешно изтрита.";
            return RedirectToAction("UnitDepartmentList");
        }

        [HttpGet]
        public IActionResult DeletedUnitDepartments(int page)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin;

            var UnitDepartments = _departmentService.GetDeletedUnitDepartmentsPaged(page);
            return View(UnitDepartments);
        }

        [HttpPost]
        public IActionResult RestoreDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var department = _departmentService.GetDeletedDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("DeletedUnitDepartments");
            }

            _departmentService.RestoreDepartment(department);
            _logService.AddLog(loggedUser, LogAction.Restore, department);

            TempData["Success"] = "Дирекцията е успешно възстановенa.";
            return RedirectToAction("DeletedUnitDepartments");
        }

        [HttpPost]
        public IActionResult HardDeleteDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var department = _departmentService.GetDeletedDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = "Дирекцията не е намеренa";
                return RedirectToAction("DeletedUnitDepartments");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, department);
            _departmentService.HardDeleteDepartment(department);

            TempData["Success"] = "Дирекцията е успешно изтритa.";
            return RedirectToAction("DeletedUnitDepartments");
        }
    }
}
