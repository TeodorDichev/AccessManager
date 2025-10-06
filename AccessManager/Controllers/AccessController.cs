using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.Directive;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;
        private readonly UnitService _unitService;
        private readonly DepartmentService _departmentService;
        private readonly PositionService _positionService;
        private readonly UserAccessService _userAccessService;

        public AccessController(Context context, UserService userService, LogService logService,
            AccessService accessService, DirectiveService directiveService, UserAccessService userAccessService,
            UnitService unitService, DepartmentService departmentService, PositionService positionService)
        {
            _logService = logService;
            _userService = userService;
            _accessService = accessService;
            _directiveService = directiveService;
            _userAccessService = userAccessService;
            _unitService = unitService;
            _departmentService = departmentService;
            _positionService = positionService;
        }

        [HttpGet]
        public IActionResult AccessList(AccessListViewModel model, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var result = new AccessListViewModel
            {
                Accesses = _accessService.GetAccessesPaged(_accessService.GetAccess(model.FilterAccessId), page),
                LoggedUserWriteAuthority = loggedUser.WritingAccess,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                FilterAccessId = model.FilterAccessId,
                FilterAccessDescription = _accessService.GetAccess(model.FilterAccessId)?.FullDescription ?? "-"
            };

            return View(result);
        }

        [HttpGet]
        public IActionResult CreateAccess(Guid? parentAccessId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.Restricted)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var model = new CreateAccessViewModel();

            if (parentAccessId != null)
            {
                Access? parent = _accessService.GetAccess(parentAccessId);
                if(parent != null)
                {
                    model = new CreateAccessViewModel()
                    {
                        ParentAccessId = parentAccessId,
                        ParentDescription = parent.FullDescription,
                        Level = parent.Level
                    };

                }
            }
            return View("CreateAccess", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAccess(CreateAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid) return View("CreateAccess", model);

            if (model.Level > 0 && !model.ParentAccessId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ParentAccessId), ExceptionMessages.ChooseParentAccess);
                return View("CreateAccess", model);
            }

            if (model.ParentAccessId.HasValue)
            {
                var all = _accessService.GetAccesses();
                var dict = all.ToDictionary(a => a.Id, a => a);

                if (!dict.TryGetValue(model.ParentAccessId.Value, out var parent))
                {
                    ModelState.AddModelError(nameof(model.ParentAccessId), ExceptionMessages.AccessNotFound);
                    return View("CreateAccess", model);
                }

                int parentDepth = 0;
                var cur = parent;
                while (cur.ParentAccessId.HasValue)
                {
                    if (!dict.TryGetValue(cur.ParentAccessId.Value, out cur))
                    {
                        parentDepth = -1;
                        break;
                    }
                    parentDepth++;
                }
            }

            var access = new Access
            {
                Id = Guid.NewGuid(),
                Description = model.Description.Trim(),
                ParentAccessId = model.Level == 0 ? null : model.ParentAccessId,
                Level = model.Level,
                FullDescription = _accessService.GenerateAccessFullDescription(model.Description.Trim(), model.ParentAccessId),
                DeletedOn = null
            };

            _accessService.AddAccess(access);
            _logService.AddLog(loggedUser, LogAction.Add, access);
            TempData["Success"] = "Достъпът е създаден успешно.";
            return RedirectToAction("AccessList");
        }

        [HttpGet]
        public IActionResult GetParentCandidates(int level = 1, string q = "")
        {
            if (level <= 0)
                return Json(new object[0]);

            var all = _accessService.GetAccesses().Select(a => new { a.Id, a.Description, a.ParentAccessId }).ToList();

            var dict = all.ToDictionary(x => x.Id, x => x);
            var depths = new Dictionary<Guid, int>();
            foreach (var a in all)
            {
                int depth = 0;
                var cur = a;
                while (cur.ParentAccessId.HasValue)
                {
                    var pid = cur.ParentAccessId!.Value;
                    if (!dict.TryGetValue(pid, out cur))
                    {
                        depth = -1;
                        break;
                    }
                    depth++;
                }
                depths[a.Id] = depth;
            }

            int desiredParentDepth = level - 1;
            var qLower = (q ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => depths.TryGetValue(a.Id, out var d) && d == desiredParentDepth)
                .Where(a => string.IsNullOrEmpty(qLower) || a.Description.ToLowerInvariant().Contains(qLower))
                .OrderBy(a => a.Description)
                .Take(30)
                .Select(a => new { id = a.Id, text = _accessService.GetAccess(a.Id)?.FullDescription ?? "-"})
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult SearchAccesses(string term = "")
        {
            var all = _accessService.GetAccesses().Select(a => new { a.Id, a.Description }).ToList();
            var termLower = (term ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => string.IsNullOrEmpty(termLower) || a.Description.ToLowerInvariant().Contains(termLower))
                .OrderBy(a => a.Description)
                .Take(10)
                .Select(a => new { id = a.Id, text = _accessService.GetAccess(a.Id)?.FullDescription ?? "-" })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult MapUserAccess(Guid? userId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(userId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен.";
                return RedirectToAction("UserList", "User");
            }

            var vm = new MapUserAccessViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDirectiveDescription1 = "",
                FilterDirectiveDescription2 = "",
                AccessibleSystems = _accessService.GetAccessesGrantedToUserPaged(user, null, null, 1),
                InaccessibleSystems = _accessService.GetAccessesNotGrantedToUserPaged(user, null, null, 1),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult MapUserAccess(MapUserAccessViewModel model, [FromQuery(Name = "action1")] string action1, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserId);
            if (user == null)
            {
                TempData["Error"] = ExceptionMessages.UserNotFound;
                return RedirectToAction("MapUserAccess", new { userId = model.UserId });
            }

            switch (action1)
            {
                case "Grant":
                    if (!model.DirectiveToGrantAccessId.HasValue)
                    {
                        TempData["Error"] = ExceptionMessages.MissingDirective;
                        model.DirectiveToGrantAccessDescription = "";
                        break;
                    }

                    foreach (var accId in model.SelectedInaccessibleSystemIds)
                    {
                        var directive = _directiveService.GetDirective(model.DirectiveToGrantAccessId.Value);
                        var access = _accessService.GetAccess(accId);

                        if (access == null || directive == null)
                        {
                            TempData["Error"] = ExceptionMessages.GrantingAccessFailed;
                            continue;
                        }

                        var ua = _userAccessService.AddUserAccess(user, access, directive);
                        _logService.AddLog(loggedUser, LogAction.Add, ua);
                    }
                    model.SelectedInaccessibleSystemIds = new();
                    break;

                case "Revoke":
                    if (!model.DirectiveToRevokeAccessId.HasValue)
                    {
                        TempData["Error"] = ExceptionMessages.MissingDirective;
                        model.DirectiveToRevokeAccessDescription = "";
                        break;
                    }

                    foreach (var accId in model.SelectedAccessibleSystemIds)
                    {
                        var directive = _directiveService.GetDirective(model.DirectiveToRevokeAccessId.Value);
                        var userAccess = _userAccessService.GetUserAccess(user.Id, accId);

                        if (userAccess == null || directive == null)
                        {
                            TempData["Error"] = ExceptionMessages.RevokingAccessFailed;
                            continue;
                        }

                        _userAccessService.RevokeUserAccess(userAccess, directive);
                        _logService.AddLog(loggedUser, LogAction.Edit, userAccess);
                    }
                    model.SelectedAccessibleSystemIds = new();
                    break;
            }

            var filterDirective1 = _directiveService.GetDirective(model.FilterDirectiveId1);
            var filterDirective2 = _directiveService.GetDirective(model.FilterDirectiveId2);

            var filterAccess1 = _accessService.GetAccess(model.FilterAccessId1);
            var filterAccess2 = _accessService.GetAccess(model.FilterAccessId2);

            var vm = new MapUserAccessViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDirectiveDescription1 = filterDirective1?.Name ?? "",
                FilterDirectiveDescription2 = filterDirective2?.Name ?? "",
                FilterDirectiveId1 = model.FilterDirectiveId1,
                FilterDirectiveId2 = model.FilterDirectiveId2,
                FilterAccessDescription1 = filterAccess1?.Description ?? "",
                FilterAccessDescription2 = filterAccess2?.Description ?? "",
                FilterAccessId1 = model.FilterAccessId1,
                FilterAccessId2 = model.FilterAccessId2,
                AccessibleSystems = _accessService.GetAccessesGrantedToUserPaged(user, filterDirective1, filterAccess1, page1),
                InaccessibleSystems = _accessService.GetAccessesNotGrantedToUserPaged(user, filterDirective2, filterAccess2, page2),
                SelectedAccessibleSystemIds = model.SelectedAccessibleSystemIds,
                SelectedInaccessibleSystemIds = model.SelectedInaccessibleSystemIds,
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult EditAccess(Guid? accessId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("AccessList");
            }

            var vm = new EditAccessViewModel
            {
                AccessId = access.Id,
                Name = access.Description,
                Description = access.FullDescription,
                FilterDirectiveDescription1 = "",
                FilterDirectiveDescription2 = "",
                UsersWithAccess = _userAccessService.GetUsersWithAccessPaged(loggedUser, access, null, 1),
                UsersWithoutAccess = _userAccessService.GetUsersWithoutAccessPaged(loggedUser, access, null, 1),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(vm);

        }

        [HttpPost]
        public IActionResult EditAccess(EditAccessViewModel model, [FromQuery(Name = "action1")] string action1, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("AccessList");
            }

            switch (action1)
            {
                case "Grant":
                    if (access == null || !model.DirectiveToGrantAccessId.HasValue)
                    {
                        TempData["Error"] = ExceptionMessages.MissingDirective;
                        return RedirectToAction("EditAccess", new { accessId = model.AccessId });
                    }

                    foreach (var userId in model.SelectedUsersWithoutAccessIds)
                    {
                        var directive = _directiveService.GetDirective(model.DirectiveToGrantAccessId.Value);
                        var user = _userService.GetUser(userId);
                        if (user == null || directive == null)
                        {
                            TempData["Error"] = ExceptionMessages.GrantingAccessFailed;
                            continue;
                        }

                        UserAccess ua = _userAccessService.AddUserAccess(user, access, directive);
                        _logService.AddLog(loggedUser, LogAction.Add, ua);
                    }

                    model.SelectedUsersWithoutAccessIds = new();
                    break;
                case "Revoke":
                    if (access == null || !model.DirectiveToRevokeAccessId.HasValue)
                    {
                        TempData["Error"] = ExceptionMessages.MissingDirective;
                        return RedirectToAction("EditAccess", new { accessId = model.AccessId });
                    }

                    foreach (var userId in model.SelectedUsersWithAccessIds)
                    {
                        var directive = _directiveService.GetDirective(model.DirectiveToRevokeAccessId.Value);
                        UserAccess? userAccess = _userAccessService.GetUserAccess(userId, access.Id);
                        if (userAccess == null || directive == null)
                        {
                            TempData["Error"] = ExceptionMessages.RevokingAccessFailed;
                            continue;
                        }
                        _userAccessService.RevokeUserAccess(userAccess, directive);
                        _logService.AddLog(loggedUser, LogAction.Edit, userAccess);
                    }

                    model.SelectedUsersWithAccessIds = new();
                    break;

            }

            var filterDirective1 = _directiveService.GetDirective(model.FilterDirectiveId1);
            var filterDirective2 = _directiveService.GetDirective(model.FilterDirectiveId2);

            var vm = new EditAccessViewModel
            {
                AccessId = access.Id,
                Name = access.Description,
                Description = access.FullDescription,
                FilterDirectiveDescription1 = filterDirective1?.Name ?? "",
                FilterDirectiveDescription2 = filterDirective2?.Name ?? "",
                FilterDirectiveId1 = model.FilterDirectiveId1,
                FilterDirectiveId2 = model.FilterDirectiveId2,
                SelectedUsersWithAccessIds = model.SelectedUsersWithAccessIds,
                SelectedUsersWithoutAccessIds = model.SelectedUsersWithoutAccessIds,
                UsersWithAccess = _userAccessService.GetUsersWithAccessPaged(loggedUser, access, filterDirective1, page1),
                UsersWithoutAccess = _userAccessService.GetUsersWithoutAccessPaged(loggedUser, access, filterDirective2, page2),
                LoggedUserReadAuthority = loggedUser.ReadingAccess,
                LoggedUserWriteAuthority = loggedUser.WritingAccess
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult UpdateUserDirective([FromBody] UpdateUserAccessDirectiveViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null)
                return RedirectToAction("Login", "Home");

            var userAccess = _userAccessService.GetUserAccess(model.UserId, model.AccessId);
            var directive = _directiveService.GetDirective(model.DirectiveId);

            IActionResult RedirectWithError(string errorMessage)
            {
                TempData["Error"] = errorMessage;
                return model.RedirectTo switch
                {
                    "EditAccess" => RedirectToAction(model.RedirectTo, new { accessId = model.AccessId }),
                    "MapUserAccess" => RedirectToAction(model.RedirectTo, new { userId = model.UserId }),
                    _ => RedirectToAction("AccessList")
                };
            }

            if (userAccess == null) return RedirectWithError(ExceptionMessages.AccessNotFound);
            if (directive == null) return RedirectWithError(ExceptionMessages.DirectiveNotFound);

            var ua = _userAccessService.UpdateUserAccessDirective(userAccess, directive);
            _logService.AddLog(loggedUser, LogAction.Edit, ua);

            return model.RedirectTo switch
            {
                "EditAccess" => RedirectToAction(model.RedirectTo, new { accessId = model.AccessId }),
                "MapUserAccess" => RedirectToAction(model.RedirectTo, new { userId = model.UserId }),
                _ => RedirectToAction("AccessList")
            };
        }

        [HttpPost]
        public IActionResult EditAccessName(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess <= AuthorityType.Restricted)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("AccessList");
            }

            if (!string.IsNullOrEmpty(model.Name))
            {
                _accessService.UpdateAccessName(model.Name, access);
                _logService.AddLog(loggedUser, LogAction.Edit, access);
            }

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpGet]
        public IActionResult UserAccessList(UserAccessListViewModel model, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.FilterUserId);
            var access = _accessService.GetAccess(model.FilterAccessId);
            var directive = _directiveService.GetDirective(model.FilterDirectiveId);
            var unit = _unitService.GetUnit(model.FilterUnitId);
            var department = _departmentService.GetDepartment(model.FilterDepartmentId);
            var position = _positionService.GetPosition(model.FilterPositionId);

            model.LoggedUserWriteAuthority = loggedUser.WritingAccess;
            model.LoggedUserReadAuthority = loggedUser.ReadingAccess;
            model.FilterUserName = user == null ? "" : user.UserName;
            model.FilterAccessDescription = access == null ? "" : access.FullDescription;
            model.FilterDirectiveDescription = directive == null ? "" : directive.Name;
            model.FilterUnitDescription = unit == null ? "" : unit.Description;
            model.FilterDepartmentDescription = department == null ? "" : department.Description;
            model.FilterPositionDescription = position == null ? "" : position.Description;

            model.UserAccessList = _userAccessService.GetUserAccessesPaged(loggedUser, user, access, directive, department, unit, position,model.SelectedSortOption, page);

            return View(model);
        }

        [HttpPost]
        public IActionResult SoftDeleteAccess(Guid id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.Full)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var accessToDelete = _accessService.GetAccess(id);
            if (accessToDelete == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("AccessList");
            }
            else if (!_accessService.CanDeleteAccess(accessToDelete))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeDeletedDueToDependencies;
                return RedirectToAction("DeletedAccesses");
            }

            _logService.AddLog(loggedUser, LogAction.Delete, accessToDelete);
            _accessService.SoftDeleteAccess(accessToDelete);
            TempData["Success"] = "Достъпът е успешно изтрит.";
            return RedirectToAction("AccessList");
        }

        [HttpGet]
        public IActionResult DeletedAccesses(int page)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.ReadingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var result = new PagedResult<AccessListItemViewModel>
            {
                Items = _accessService.GetDeletedAccesses(page).Select(o => new AccessListItemViewModel
                {
                    AccessId = o.Id,
                    Description = o.FullDescription
                })
                .ToList(),

                Page = page,
                TotalCount = _accessService.GetDeletedAccessesCount()
            };

            return View(result);
        }

        [HttpPost]
        public IActionResult RestoreAccess(Guid accessId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var access = _accessService.GetDeletedAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("DeletedAccesses");
            }
            else if (!_accessService.CanRestoreAccess(access))
            {
                TempData["Error"] = ExceptionMessages.EntityCannotBeRestoredDueToDeletedDependencies;
                return RedirectToAction("DeletedAccesses");
            }

            _accessService.RestoreAccess(access);
            _logService.AddLog(loggedUser, LogAction.Restore, access);

            TempData["Success"] = "Достъпът е успешно възстановен.";
            return RedirectToAction("DeletedAccesses");
        }

        [HttpPost]
        public IActionResult HardDeleteAccess(Guid accessId)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");
            if (loggedUser.WritingAccess < AuthorityType.SuperAdmin)
            {
                TempData["Error"] = ExceptionMessages.InsufficientAuthority;
                return RedirectToAction("AccessList");
            }

            var access = _accessService.GetDeletedAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = ExceptionMessages.AccessNotFound;
                return RedirectToAction("DeletedAccesses");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, access);
            _accessService.HardDeleteAccess(access);

            TempData["Success"] = "Достъпът е успешно изтрит.";
            return RedirectToAction("DeletedAccesses");
        }
    }
}
