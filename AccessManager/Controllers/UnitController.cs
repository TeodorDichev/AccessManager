using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
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
        public UnitController(UnitService unitService, UserService userService, LogService logService)
        {
            _unitService = unitService;
            _userService = userService;
            _logService = logService;
        }

        [HttpGet]
        public ActionResult EditUnit(string id)
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

            UnitEditViewModel model = new UnitEditViewModel
            {
                UnitId = unit.Id,
                DepartmentName = unit.Department.Description,
                UnitName = unit.Description,
                UsersWithAccess = unit.UsersWithAccess
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

            Unit? unit = _unitService.GetUnit(model.UnitId.ToString());
            if (unit == null)
            {
                TempData["Error"] = "Отделът не е намерен";
                return RedirectToAction("EditUnit", new { model.UnitId });
            }

            unit.Description = model.UnitName;
            _userService.SaveChanges();
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
            _unitService.RemoveUnitUser(uu);
            return Json(new { success = true, message = "Достъпът е премахнат успешно" });
        }

        [HttpGet]
        public IActionResult CreateUnit(string departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            CreateUnitViewModel model = new CreateUnitViewModel();

            if (!string.IsNullOrEmpty(departmentId)) model.DepartmentId = Guid.Parse(departmentId);

            var departments = _userService.GetAllowedDepartments(loggedUser).Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description }).ToList();

            model.Departments = departments;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUnit(CreateUnitViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid) return View(model);

            Unit uu = _unitService.CreateUnit(model.UnitName, model.DepartmentId);
            _logService.AddLog(loggedUser, LogAction.Add, uu);
            return RedirectToAction("UnitDepartmentList", "Department");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUnit(string unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Unit? unit = _unitService.GetUnit(unitId);
            if(unit != null)
            {
                _logService.AddLog(loggedUser, LogAction.Delete, unit);
                _unitService.SoftDeleteUnit(unitId);
            }
            else TempData["Error"] = "Отделът не е изтрит успешно";

            return RedirectToAction("UnitDepartmentList", "Department");
        }

        [HttpGet]
        public IActionResult MapUserUnitAccess(string username, string filterDepartment1, string filterDepartment2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(username);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("EditUser", new { username });
            }

            var departments = _userService.GetAllowedDepartments(loggedUser).OrderBy(d => d.Description)
                                    .Select(d => new SelectListItem(d.Description, d.Id.ToString()))
                                    .ToList();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var accessibleUnitsQuery = _userService.GetUserAccessibleUnits(user, loggedUser);
            var inaccessibleUnitsQuery = _userService.GetUserInaccessibleUnits(user, loggedUser);

            if (!string.IsNullOrEmpty(filterDepartment1))
                accessibleUnitsQuery = accessibleUnitsQuery.Where(u => u.Department.Id == Guid.Parse(filterDepartment1)).ToList();
            if (!string.IsNullOrEmpty(filterDepartment2))
                inaccessibleUnitsQuery = inaccessibleUnitsQuery.Where(u => u.Department.Id == Guid.Parse(filterDepartment2)).ToList();

            var totalAccessible = accessibleUnitsQuery.Count();
            var totalInaccessible = inaccessibleUnitsQuery.Count();

            var accessibleUnits = accessibleUnitsQuery
                .OrderBy(u => u.Description)
                .Skip((page1 - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(u => new UnitViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Description,
                    DepartmentName = u.Department.Description
                })
                .ToList();

            var inaccessibleUnits = inaccessibleUnitsQuery
                .OrderBy(u => u.Description)
                .Skip((page2 - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(u => new UnitViewModel
                {
                    UnitId = u.Id,
                    UnitName = u.Description,
                    DepartmentName = u.Department.Description
                })
                .ToList();

            var vm = new MapUserUnitAccessViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDepartments = departments,
                FilterDepartment1 = filterDepartment1,
                FilterDepartment2 = filterDepartment2,
                AccessibleUnits = accessibleUnits,
                InaccessibleUnits = inaccessibleUnits,
                CurrentPage1 = page1,
                TotalPages1 = (int)Math.Ceiling(totalAccessible / (double)Constants.ItemsPerPage),
                CurrentPage2 = page2,
                TotalPages2 = (int)Math.Ceiling(totalInaccessible / (double)Constants.ItemsPerPage)
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
                var uu = _unitService.AddUnitAccess(user.Id, unitId);
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
                    _unitService.RemoveUnitUser(uu);
                }
            }

            return RedirectToAction("MapUserUnitAccess", new { model.UserName });
        }
    }
}
