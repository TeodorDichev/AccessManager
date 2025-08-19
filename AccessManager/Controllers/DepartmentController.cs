using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
        public ActionResult UnitDepartmentList(string filterDepartment, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            List<UnitDepartmentViewModel> departments = _departmentService.GetAllowedDepartments(loggedUser)
                .Select(g => new UnitDepartmentViewModel
                {
                    DepartmentId = g.Id,
                    DepartmentName = g.Description,
                }).ToList();

            List<UnitDepartmentViewModel> units = _unitService.GetUserAllowedUnits(loggedUser)
                .Select(g => new UnitDepartmentViewModel
                {
                    DepartmentId = g.Id,
                    DepartmentName = g.Description,
                }).ToList();

            List<UnitDepartmentViewModel> list = departments.Concat(units).ToList();

            if (!string.IsNullOrEmpty(filterDepartment))
                list = list.Where(d => d.DepartmentName == filterDepartment).ToList();

            int totalUsers = list.Count;

            UnitDepartmentListViewModel model = new UnitDepartmentListViewModel
            {
                UnitDepartments = list,
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
                Units = _unitService.GetAllowedUnitsForDepartment(loggedUser, Guid.Parse(id)).Select(u => new UnitViewModel { UnitId = u.Id, UnitName = u.Description }).ToList(),
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

            var res = _unitService.GetAllowedUnitsForDepartment(loggedUser, departmentId).Select(u => new { UnitId = u.Id, u.Description });
            return Json(res);
        }

        [HttpGet]
        public IActionResult GetAccessibleDepartmentsForUser()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var departments = _unitService.GetUserAllowedUnits(loggedUser)
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
            else if(!_departmentService.CanDeleteDepartment(dep))
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

            var model = new DeletedUnitDepartmentListViewModel()
            {
                UnitDepartments = _departmentService.GetDeletedUnitDepartments(page).ToList(),
                CurrentPage = page,
                TotalPages = _departmentService.GetDeletedUnitDepartmentsCount(),
            };
            return View(model);
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
                return RedirectToAction("DeletedAccesses");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, department);
            _departmentService.HardDeleteDepartment(department);

            TempData["Success"] = "Дирекцията е успешно изтритa.";
            return RedirectToAction("DeletedUnitDepartments");
        }
    }
}
