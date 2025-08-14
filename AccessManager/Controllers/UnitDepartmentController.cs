using AccessManager.Data.Entities;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.UnitDepartment;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

            if(!string.IsNullOrEmpty(filterDepartment))
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

            Department? dep = _departmentUnitService.GetDepartment(id);
            if (dep == null) return NotFound();

            if (loggedUser.WritingAccess < Data.Enums.AuthorityType.Full)
            {
                ViewBag.IsReadOnly = true;
            }

            DepartmentEditViewModel model = new DepartmentEditViewModel
            {
                DepartmentId = dep.Id,
                DepartmentName = dep.Description,
                Units = _userService.GetAllowedUnitsForDepartment(loggedUser, Guid.Parse(id)).Select(u => new UnitListItemViewModel { UnitId = u.Id, UnitName = u.Description}).ToList(),
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

            Department? dep = _departmentUnitService.GetDepartment(model.DepartmentId.ToString());
            if (dep == null) return NotFound();

            if (_departmentUnitService.DepartmentWithDescriptionExists(model.DepartmentName)) ModelState.AddModelError("", "Дирекция с това име съществува");
            dep.Description = model.DepartmentName;
            _userService.SaveChanges();

            return View(model);
        }

        [HttpGet]
        public ActionResult EditUnit(string id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            Unit? unit = _departmentUnitService.GetUnit(id);
            if (unit == null) return NotFound();

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

            Unit? unit = _departmentUnitService.GetUnit(model.UnitId.ToString());
            if (unit == null) return NotFound();

            unit.Description = model.UnitName;
            _userService.SaveChanges();

            return View(model);
        }

        [HttpPost]
        public IActionResult RemoveUnitAccess(string username, Guid unitId)
        {
            var user = _userService.GetUser(username);
            if (user == null) return Json(new { success = false, message = "User not found" });
            else if (user.WritingAccess == Data.Enums.AuthorityType.SuperAdmin) return Json(new { success = false, message = "Cannot remove unit access from superadmin" });

            if (user.WritingAccess == Data.Enums.AuthorityType.Full) user.WritingAccess = Data.Enums.AuthorityType.Restricted;
            if (user.ReadingAccess == Data.Enums.AuthorityType.Full) user.ReadingAccess = Data.Enums.AuthorityType.Restricted;

            _departmentUnitService.RemoveUserUnit(user.Id, unitId);
            return Json(new { success = true, message = "Достъпът е премахнат успешно" });
        }

        [HttpGet]
        public IActionResult GetAccessibleUnitsForUserDepartment(Guid departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var res = _userService.GetAllowedUnitsForDepartment(loggedUser, departmentId).Select(u => new {UnitId = u.Id, u.Description });
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

        [HttpGet]
        public IActionResult CreateUnit(string departmentId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            CreateUnitViewModel model = new CreateUnitViewModel();

            if (!string.IsNullOrEmpty(departmentId)) {
                model.DepartmentId = Guid.Parse(departmentId);
            }

            var departments = _userService.GetAllowedDepartments(loggedUser).Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description }).ToList();

            model.Departments = departments;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUnit(CreateUnitViewModel model)
        {
            if(!ModelState.IsValid) return View(model);

            _departmentUnitService.CreateUnit(model.UnitName, model.DepartmentId);
            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNewDepartment(string DepartmentName)
        {
            _departmentUnitService.CreateDepartment(DepartmentName);
            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteDepartment(string departmentId)
        {
            _departmentUnitService.SoftDeleteDepartment(departmentId);
            return RedirectToAction("UnitDepartmentList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUnit(string unitId)
        {
            _departmentUnitService.SoftDeleteUnit(unitId);
            return RedirectToAction("UnitDepartmentList");
        }
    }
}
