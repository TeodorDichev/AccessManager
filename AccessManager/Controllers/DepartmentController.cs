using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
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

            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var results = _departmentService.GetDepartmentsByUserAuthority(loggedUser, loggedUser.WritingAccess)
                .Where(d => string.IsNullOrEmpty(term) || d.Description.ToLowerInvariant().Contains(termLower))
                .Select(d => new { id = d.Id, text = d.Description })
                .Take(10)
                .ToList();

            return Json(results);
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

        [HttpGet]
        public ActionResult UnitDepartmentList(UnitDepartmentListViewModel model, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var dep = _departmentService.GetDepartment(model.FilterDepartmentId);
            var unit = _unitService.GetUnit(model.FilterUnitId);

            UnitDepartmentListViewModel result = new UnitDepartmentListViewModel
            {
                UnitDepartments = _departmentService.GetUnitDepartmentsPaged(loggedUser, dep, unit, page),
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                FilterDepartmentId = model.FilterDepartmentId,
                FilterDepartmentDescription = dep?.Description ?? "",
                FilterUnitId = model.FilterUnitId,
                FilterUnitDescription = unit?.Description ?? "",
            };

            return View(result);
        }

        [HttpGet]
        public ActionResult EditDepartment(Guid? id, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Department? dep = _departmentService.GetDepartment(id);
            if (dep == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("UnitDepartmentList");
            }

            DepartmentEditViewModel model = new DepartmentEditViewModel
            {
                DepartmentId = dep.Id,
                DepartmentName = dep.Description,
                Units = _unitService.GetUserUnitsForDepartmentPaged(loggedUser, dep, page),
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
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
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("EditDepartment", new { model.DepartmentId });
            }

            if (_departmentService.DepartmentWithNameExists(model.DepartmentName))
            {
                TempData["Error"] = ExceptionMessages.DepartmentWithNameExists;
                return View(model);
            }

            _departmentService.UpdateDepartmentName(dep, model.DepartmentName);
            _logService.AddLog(loggedUser, Data.Enums.LogAction.Edit, dep);

            return RedirectToAction("EditDepartment", new { model.DepartmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNewDepartment(string DepartmentName)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (_departmentService.DepartmentWithNameExists(DepartmentName))
            {
                TempData["Error"] = ExceptionMessages.DepartmentWithNameExists;
                return RedirectToAction("UnitDepartmentList");
            }

            Department dep = _departmentService.CreateDepartment(DepartmentName);
            _logService.AddLog(loggedUser, LogAction.Add, dep);

            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if(loggedUser.WritingAccess < Data.Enums.AuthorityType.Full)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UnitDepartmentList");
            }

            Department? dep = _departmentService.GetDepartment(departmentId);
            if (dep == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("UnitDepartmentList");
            }
            else if (!_departmentService.CanDeleteDepartment(dep))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeDeletedDueToDependencies;
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
            if (loggedUser.ReadingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var UnitDepartments = _departmentService.GetDeletedUnitDepartmentsPaged(page);
            return View(UnitDepartments);
        }

        [HttpPost]
        public IActionResult RestoreDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var department = _departmentService.GetDeletedDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
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
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var department = _departmentService.GetDeletedDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("DeletedUnitDepartments");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, department);
            _departmentService.HardDeleteDepartment(department);

            TempData["Success"] = "Дирекцията е успешно изтритa.";
            return RedirectToAction("DeletedUnitDepartments");
        }
    }
}
