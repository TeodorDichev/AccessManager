using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.Controllers
{
    public class UnitController : BaseController
    {
        private readonly LogService _logService;
        private readonly UnitService _unitService;
        private readonly UserService _userService;
        private readonly DepartmentService _departmentService;
        public UnitController(UnitService unitService, UserService userService, LogService logService, DepartmentService departmentService)
        {
            _unitService = unitService;
            _userService = userService;
            _logService = logService;
            _departmentService = departmentService;
        }

        [HttpGet]
        public ActionResult EditUnit(Guid id, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            Unit? unit = _unitService.GetUnit(id);
            if (unit == null)
            {
                TempData["Error"] = "Отделът не е намерена";
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            var pagedRes = new PagedResult<UserListItemViewModel>
            {
                Items = unit.UsersWithAccess
                    .Where(u => u.User != null && loggedUser.AccessibleUnits.Select(au => au.UnitId).Contains(u.User.UnitId))
                    .Select(u => new UserListItemViewModel
                    {
                        UserName = u.User.UserName,
                        FirstName = u.User.FirstName,
                        LastName = u.User.LastName,
                        Department = u.User.Unit.Department.Description,
                        Unit = u.User.Unit.Description,
                        ReadAccess = u.User.ReadingAccess,
                        WriteAccess = u.User.WritingAccess
                    }).ToList(),
                Page = page,
                TotalCount = unit.UsersWithAccess.Count(u => u.User != null && loggedUser.AccessibleUnits.Select(au => au.UnitId).Contains(u.User.UnitId))
            };

            UnitEditViewModel model = new UnitEditViewModel
            {
                UnitId = unit.Id,
                DepartmentName = unit.Department.Description,
                UnitName = unit.Description,
                UsersWithAccess = pagedRes,
                WriteAuthority = loggedUser.WritingAccess
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EditUnit(UnitEditViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            if (!ModelState.IsValid) return View(model);

            Unit? unit = _unitService.GetUnit(model.UnitId);
            if (unit == null)
            {
                TempData["Error"] = "Отделът не е намерен";
                return RedirectToAction("EditUnit", new { model.UnitId });
            }

            _unitService.UpdateUnitName(unit, model.UnitName);
            _logService.AddLog(loggedUser, LogAction.Edit, unit);

            return View(model);
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(string username, Guid unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(username);
            if (user == null) return Json(new { success = false, message = "User not found" });

            var uu = _unitService.GetUnitUser(user.Id, unitId);

            if(uu == null) return Json(new { success = false, message = "Not Found" });
            if (user.WritingAccess == Data.Enums.AuthorityType.SuperAdmin) return Json(new { success = false, message = "Cannot remove unit access from superadmin" });

            if (user.WritingAccess == Data.Enums.AuthorityType.Full) user.WritingAccess = Data.Enums.AuthorityType.Restricted;
            if (user.ReadingAccess == Data.Enums.AuthorityType.Full) user.ReadingAccess = Data.Enums.AuthorityType.Restricted;

            _logService.AddLog(loggedUser, LogAction.Delete, uu);
            _unitService.HardDeleteUnitUser(uu);
            return Json(new { success = true, message = "Достъпът е премахнат успешно" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUnit(CreateUnitViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

           var department = _departmentService.GetDepartment(model.DepartmentId);
            if (department == null)
            {
                TempData["Error"] = "Дирекцията не е намерена";
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            Unit uu = _unitService.CreateUnit(model.UnitName, department);
            _logService.AddLog(loggedUser, LogAction.Add, uu);

            return RedirectToAction("UnitDepartmentList", "Department");
        }

        [HttpGet]
        public IActionResult MapUserUnitAccess(Guid userId, Guid? filterDepartmentId1, Guid? filterDepartmentId2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(userId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("EditUser", new { userId });
            }

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var filterDepartment1 = _departmentService.GetDepartment(filterDepartmentId1);
            var filterDepartment2 = _departmentService.GetDepartment(filterDepartmentId2);

            var vm = new MapUserUnitAccessViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDepartmentId1 = filterDepartmentId1,
                FilterDepartmentId2 = filterDepartmentId2,
                FilterDepartmentDescription1 = filterDepartment1?.Description ?? "",
                FilterDepartmentDescription2 = filterDepartment2?.Description ?? "",
                AccessibleUnits = _unitService.GetAccessibleUnitsPaged(loggedUser, user, filterDepartment1, page1),
                InaccessibleUnits = _unitService.GetInaccessibleUnitsPaged(loggedUser, user, filterDepartment1, page1),
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult GrantUnitAccess(MapUserUnitAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserName);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("MapUserUnitAccess", new { model.UserName });
            }

            foreach (var unitId in model.SelectedInaccessibleUnitIds)
            {
                var unit = _unitService.GetUnit(unitId);
                if (unit == null) 
                {
                    TempData["Error"] = "Отделът не е намерен";
                    continue; 
                }

                var uu = _unitService.AddUnitUser(user, unit);
                _logService.AddLog(loggedUser, LogAction.Add, uu);
            }

            return RedirectToAction("MapUserUnitAccess", new { model.UserName });
        }

        [HttpPost]
        public IActionResult RevokeUnitAccess(MapUserUnitAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserName);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("MapUserUnitAccess", new { model.UserName });
            }

            if (model.SelectedAccessibleUnitIds.Count > 0 && (user.WritingAccess == AuthorityType.Full || user.ReadingAccess == AuthorityType.Full))
            {
                user.WritingAccess = AuthorityType.Restricted;
                user.ReadingAccess = AuthorityType.Restricted;
            }


            foreach (var unitId in model.SelectedAccessibleUnitIds)
            {
                var uu = _unitService.GetUnitUser(user.Id, unitId);
                if(uu != null)
                {
                    _logService.AddLog(loggedUser, LogAction.Add, uu);
                    _unitService.HardDeleteUnitUser(uu);
                }
            }

            return RedirectToAction("MapUserUnitAccess", new { model.UserName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUnit(Guid? unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Unit? unit = _unitService.GetUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = "Не съществува такъв отдел";
                return RedirectToAction("UnitDepartmentList", "Department");
            }
            else if (!_unitService.CanDeleteUnit(unit))
            {
                TempData["Error"] = "Отделът не е изтрит успешно";
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            TempData["Success"] = "Отделът е изтрит успешно";
            _logService.AddLog(loggedUser, LogAction.Delete, unit);
            _unitService.SoftDeleteUnit(unit);
            return RedirectToAction("UnitDepartmentList", "Department");
        }

        [HttpPost]
        public IActionResult RestoreUnit(Guid unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var unit = _unitService.GetDeletedUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = "Отделът не е намерена";
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }
            else if (!_unitService.CanRestoreUnit(unit))
            {
                TempData["Error"] = "Отделът не е може да бъде възстановен понеже не съществува съответната дирекция";
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }

            _unitService.RestoreUnit(unit);
            _logService.AddLog(loggedUser, LogAction.Restore, unit);

            TempData["Success"] = "Дирекцията е успешно възстановенa.";
            return RedirectToAction("DeletedUnitDepartments", "Department");
        }

        [HttpPost]
        public IActionResult HardDeleteUnit(Guid unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var unit = _unitService.GetDeletedUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = "Отделът не е намеренa";
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, unit);
            _unitService.HardDeleteUnit(unit);

            TempData["Success"] = "Дирекцията е успешно изтритa.";
            return RedirectToAction("DeletedUnitDepartments", "Department");
        }

    }
}
