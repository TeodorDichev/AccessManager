using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.Department;
using AccessManager.ViewModels.Unit;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult SearchDepartmentUnits(Guid? departmentId, string term)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var department = _departmentService.GetDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var results = _unitService.GetUserUnitsForDepartment(loggedUser, department)
                .Where(d => string.IsNullOrEmpty(term) || d.Description.ToLowerInvariant().Contains(termLower))
                .DistinctBy(u => u.Description)
                .Select(u => new { id = u.Id, text = u.Description })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public IActionResult SearchUnits(string term)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var results = loggedUser.AccessibleUnits
                .Where(uu => string.IsNullOrEmpty(term) || uu.Unit.Description.Contains(term))
                .DistinctBy(uu => uu.Unit.Description)
                .Select(uu => new { id = uu.UnitId, text = uu.Unit.Description })
                .Take(10)
                .ToList();

            return Json(results);
        }

        [HttpGet]
        public IActionResult GetAccessibleUnits(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessibleUnits = loggedUser.AccessibleUnits.Select(au => new UnitDepartmentViewModel
            {
                UnitName = au.Unit.Description,
                DepartmentName = au.Unit.Department.Description
            }).ToList();

            var totalCount = accessibleUnits.Count();

            var paged = accessibleUnits
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

            var result = new PagedResult<UnitDepartmentViewModel>
            {
                Items = paged,
                Page = page,
                TotalCount = totalCount
            };
            return PartialView("_AccessibleUnitsTable", result);
        }

        [HttpGet]
        public ActionResult EditUnit(Guid? id, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            Unit? unit = _unitService.GetUnit(id);
            if (unit == null)
            {
                TempData["Error"] = ExceptionMessages.UnitNotFound;
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            var pagedRes = new PagedResult<UserListItemViewModel>
            {
                Items = unit.UsersWithAccess
                    .Where(u => u.User != null && loggedUser.AccessibleUnits.Select(au => au.UnitId).Contains(u.User.UnitId))
                    .Select(u => new UserListItemViewModel
                    {
                        Id = u.User.Id,
                        Position = u.User.Position?.Description ?? "",
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
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EditUnit(UnitEditViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid) return View(model);

            Unit? unit = _unitService.GetUnit(model.UnitId);
            if (unit == null)
            {
                TempData["Error"] = ExceptionMessages.UnitNotFound;
                return RedirectToAction("EditUnit", new { model.UnitId });
            }

            _unitService.UpdateUnitName(unit, model.UnitName);
            _logService.AddLog(loggedUser, LogAction.Edit, unit);

            return View(model);
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(Guid? userId, Guid? unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(userId);
            if (user == null) return Json(new { success = false, message = ExceptionMessages.UserNotFound });

            var unit = _unitService.GetUnit(userId);
            if (unit == null) return Json(new { success = false, message = ExceptionMessages.UnitNotFound });

            var uu = _unitService.GetUnitUser(user.Id, unit.Id);

            if (uu == null) return Json(new { success = false, messages = ExceptionMessages.UnitUserNotFound });
            if (user.WritingAccess >= loggedUser.WritingAccess) return Json(new { success = false, message = ExceptionMessages.InsufficientAuthority });

            if (user.WritingAccess == Data.Enums.AuthorityType.Full) user.WritingAccess = Data.Enums.AuthorityType.Restricted;
            if (user.ReadingAccess == Data.Enums.AuthorityType.Full) user.ReadingAccess = Data.Enums.AuthorityType.Restricted;

            _logService.AddLog(loggedUser, LogAction.Delete, uu);
            _unitService.HardDeleteUnitUser(uu);
            return Json(new { success = true, message = "Достъпът е премахнат успешно" });
        }

        [HttpPost]
        public IActionResult CreateUnit(CreateUnitViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var department = _departmentService.GetDepartment(model.DepartmentId);
            if (department == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            Unit uu = _unitService.CreateUnit(model.UnitName, department);
            _logService.AddLog(loggedUser, LogAction.Add, uu);

            return RedirectToAction("UnitDepartmentList", "Department");
        }

        [HttpGet]
        public IActionResult CreateUnit(Guid? departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var department = _departmentService.GetDepartment(departmentId);
            if (department == null)
            {
                TempData["Error"] = ExceptionMessages.DepartmentNotFount;
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            var model = new CreateUnitViewModel
            {
                DepartmentId = department.Id,
                DepartmentDescription = department == null ? "" : department.Description,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult MapUserUnitAccess(MapUserUnitAccessViewModel model, string action1, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserId);
            if (user == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
                return RedirectToAction("EditUser", new { model.UserId });
            }

            switch (action1)
            {
                case "Grant":
                    if(user.ReadingAccess == AuthorityType.None)
                    {
                        TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                        break;
                    }

                    foreach (var unitId in model.SelectedInaccessibleUnitIds)
                    {
                        var unit = _unitService.GetUnit(unitId);
                        if (unit == null)
                        {
                            TempData["Error"] = ExceptionMessages.UnitNotFound;
                            continue;
                        }

                        var uu = _unitService.AddUnitUser(user, unit);
                        _logService.AddLog(loggedUser, LogAction.Add, uu);
                    }
                    model.SelectedAccessibleUnitIds = new();
                    break;

                case "Revoke":
                    if (model.SelectedAccessibleUnitIds.Count > 0 && (user.WritingAccess == AuthorityType.Full || user.ReadingAccess == AuthorityType.Full))
                    {
                        user.WritingAccess = AuthorityType.Restricted;
                        user.ReadingAccess = AuthorityType.Restricted;
                    }

                    foreach (var unitId in model.SelectedAccessibleUnitIds)
                    {
                        var uu = _unitService.GetUnitUser(user.Id, unitId);
                        if (uu != null)
                        {
                            _logService.AddLog(loggedUser, LogAction.Add, uu);
                            _unitService.HardDeleteUnitUser(uu);
                        }
                    }
                    model.SelectedAccessibleUnitIds = new();

                    break;
            }

            var filterDepartment1 = _departmentService.GetDepartment(model.FilterDepartmentId1);
            var filterDepartment2 = _departmentService.GetDepartment(model.FilterDepartmentId2);

            var vm = new MapUserUnitAccessViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDepartmentDescription1 = filterDepartment1?.Description ?? "",
                FilterDepartmentDescription2 = filterDepartment1?.Description ?? "",
                FilterDepartmentId1 = model.FilterDepartmentId1,
                FilterDepartmentId2 = model.FilterDepartmentId2,
                AccessibleUnits = _unitService.GetAccessibleUnitsPaged(loggedUser, user, filterDepartment1, page1),
                InaccessibleUnits = _unitService.GetInaccessibleUnitsPaged(loggedUser, user, filterDepartment2, page2),
                SelectedAccessibleUnitIds = model.SelectedAccessibleUnitIds,
                SelectedInaccessibleUnitIds = model.SelectedInaccessibleUnitIds,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(vm);

        }

        [HttpGet]
        public IActionResult MapUserUnitAccess(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(userId);
            if (user == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
                return RedirectToAction("EditUser", new { userId });
            }

            var vm = new MapUserUnitAccessViewModel
            {
                UserId = user.Id,
                Position = user.Position?.Description ?? "",
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDepartmentDescription1 = "",
                FilterDepartmentDescription2 = "",
                AccessibleUnits = _unitService.GetAccessibleUnitsPaged(loggedUser, user, null, 1),
                InaccessibleUnits = _unitService.GetInaccessibleUnitsPaged(loggedUser, user, null, 1),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
            };

            return View(vm);
        }


        [HttpPost]
        public IActionResult SoftDeleteUnit(Guid? unitId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if(loggedUser.WritingAccess < AuthorityType.Full)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("UnitDepartmentList", "Department");
            }

            Unit? unit = _unitService.GetUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = ExceptionMessages.UnitNotFound;
                return RedirectToAction("UnitDepartmentList", "Department");
            }
            else if (!_unitService.CanDeleteUnit(unit))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeDeletedDueToDependencies;
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
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }

            var unit = _unitService.GetDeletedUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = ExceptionMessages.UnitNotFound;
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }
            else if (!_unitService.CanRestoreUnit(unit))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeRestoredDueToDeletedDependencies;
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
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }

            var unit = _unitService.GetDeletedUnit(unitId);
            if (unit == null)
            {
                TempData["Error"] = ExceptionMessages.UnitNotFound;
                return RedirectToAction("DeletedUnitDepartments", "Department");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, unit);
            _unitService.HardDeleteUnit(unit);

            TempData["Success"] = "Дирекцията е успешно изтритa.";
            return RedirectToAction("DeletedUnitDepartments", "Department");
        }
    }
}
