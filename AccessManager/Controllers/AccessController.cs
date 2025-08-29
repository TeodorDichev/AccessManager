using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;
        private readonly UserAccessService _userAccessService;

        public AccessController(Context context, UserService userService, LogService logService,
            AccessService accessService, DirectiveService directiveService, UserAccessService userAccessService)
        {
            _logService = logService;
            _userService = userService;
            _accessService = accessService;
            _directiveService = directiveService;
            _userAccessService = userAccessService;
        }

        [HttpGet]
        public IActionResult AccessList(AccessListViewModel model, int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;


            var result = new AccessListViewModel
            {
                Accesses = _accessService.GetAccessesPaged(_accessService.GetAccess(model.FilterAccessId), page),
                WriteAuthority = loggedUser.WritingAccess,
                FilterAccessId = model.FilterAccessId,
                FilterAccessDescription = _accessService.GetAccessDescription(_accessService.GetAccess(model.FilterAccessId))
            };

            return View(result);
        }

        [HttpGet]
        public IActionResult CreateAccess()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var model = new CreateAccessViewModel();
            return View("CreateAccess", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid) return View("CreateAccess", model);

            if (model.Level > 0 && !model.ParentAccessId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ParentAccessId), "Моля изберете родителски достъп за това ниво.");
                return View("CreateAccess", model);
            }

            if (model.ParentAccessId.HasValue)
            {
                var all = _accessService.GetAccesses();
                var dict = all.ToDictionary(a => a.Id, a => a);

                if (!dict.TryGetValue(model.ParentAccessId.Value, out var parent))
                {
                    ModelState.AddModelError(nameof(model.ParentAccessId), "Избраният родител не е валиден.");
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

                if (parentDepth != model.Level - 1)
                {
                    ModelState.AddModelError(nameof(model.ParentAccessId), $"Избраният родител не е на правилното ниво (очаквано ниво: {model.Level - 1}).");
                    return View("CreateAccess", model);
                }
            }

            var access = new Access
            {
                Id = Guid.NewGuid(),
                Description = model.Description.Trim(),
                ParentAccessId = model.Level == 0 ? null : model.ParentAccessId,
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
                .Select(a => new { id = a.Id, text = _accessService.GetAccessDescription(_accessService.GetAccess(a.Id)) })
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
                .Select(a => new { id = a.Id, text = _accessService.GetAccessDescription(_accessService.GetAccess(a.Id)) })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult MapUserAccess(MapUserAccessViewModel model, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var user = _userService.GetUser(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("EditUser", "User", new { model.UserId });
            }

            var filterDirective1 = _directiveService.GetDirective(model.FilterDirectiveId1);
            var filterDirective2 = _directiveService.GetDirective(model.FilterDirectiveId2);

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
                AccessibleSystems = _accessService.GetAccessesGrantedToUserPaged(user, filterDirective1, page1),
                InaccessibleSystems = _accessService.GetAccessesNotGrantedToUserPaged(user, filterDirective2, page2),
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult EditAccess(EditAccessViewModel model, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("AccessList");
            }

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var filterDirective1 = _directiveService.GetDirective(model.FilterDirectiveId1);
            var filterDirective2 = _directiveService.GetDirective(model.FilterDirectiveId2);

            var vm = new EditAccessViewModel
            {
                AccessId = access.Id,
                Name = access.Description,
                Description = _accessService.GetAccessDescription(access),
                FilterDirectiveDescription1 = filterDirective1?.Name ?? "",
                FilterDirectiveDescription2 = filterDirective2?.Name ?? "",
                FilterDirectiveId1 = model.FilterDirectiveId1,
                FilterDirectiveId2 = model.FilterDirectiveId2,
                UsersWithAccess = _userAccessService.GetUsersWithAccessPaged(loggedUser, access, filterDirective1, page1),
                UsersWithoutAccess = _userAccessService.GetUsersWithoutAccessPaged(loggedUser, access, filterDirective2, page2),
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult GrantAccessToUsers(MapUserAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserId);
            if (user == null || !model.DirectiveToGrantAccessId.HasValue)
            {
                TempData["Error"] = "Моля изберете заповед преди да дадете достъп!";
                return RedirectToAction("MapUserAccess", new { username = model.UserName });
            }

            foreach (var accId in model.SelectedInaccessibleSystemIds)
            {
                var directive = _directiveService.GetDirective(model.DirectiveToGrantAccessId.Value);
                var access = _accessService.GetAccess(accId);
                if (access == null || directive == null)
                {
                    TempData["Error"] = "Неуспешно даден достъп!";
                    continue;
                }

                UserAccess ua = _userAccessService.AddUserAccess(user, access, directive);
                _logService.AddLog(loggedUser, LogAction.Add, ua);
            }

            return RedirectToAction("MapUserAccess", new { userId = model.UserId });
        }

        [HttpPost]
        public IActionResult RevokeAccessFromUsers(MapUserAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserId);
            if (user == null || !model.DirectiveToRevokeAccessId.HasValue)
            {
                TempData["Error"] = "Моля изберете заповед преди да премахнете достъп!";
                return RedirectToAction("MapUserAccess", new { username = model.UserName });
            }

            foreach (var accId in model.SelectedAccessibleSystemIds)
            {
                var directive = _directiveService.GetDirective(model.DirectiveToRevokeAccessId.Value);
                UserAccess? userAccess = _userAccessService.GetUserAccess(user.Id, accId);
                if (userAccess == null || directive == null)
                {
                    TempData["Error"] = "Неуспешно премахнат достъп!";
                    continue;
                }
                _userAccessService.RevokeUserAccess(userAccess, directive);
                _logService.AddLog(loggedUser, LogAction.Edit, userAccess);
            }

            return RedirectToAction("MapUserAccess", new { userId = model.UserId });
        }

        [HttpPost]
        public IActionResult GrantUserAccess(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null || !model.DirectiveToGrantAccessId.HasValue)
            {
                TempData["Error"] = "Моле изберете заповед";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithoutAccessIds)
            {
                var directive = _directiveService.GetDirective(model.DirectiveToGrantAccessId.Value);
                var user = _userService.GetUser(userId);
                if (user == null || directive == null)
                {
                    TempData["Error"] = "Неуспешно даден достъп!";
                    continue;
                }

                UserAccess ua = _userAccessService.AddUserAccess(user, access, directive);
                _logService.AddLog(loggedUser, LogAction.Add, ua);
            }

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult RevokeUserAccess(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null || !model.DirectiveToRevokeAccessId.HasValue)
            {
                TempData["Error"] = "Моле изберете заповед";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithAccessIds)
            {
                var directive = _directiveService.GetDirective(model.DirectiveToRevokeAccessId.Value);
                UserAccess? userAccess = _userAccessService.GetUserAccess(userId, access.Id);
                if (userAccess == null || directive == null)
                {
                    TempData["Error"] = "Неуспешно премахнат достъп!";
                    continue;
                }
                _userAccessService.RevokeUserAccess(userAccess, directive);
                _logService.AddLog(loggedUser, LogAction.Edit, userAccess);
            }

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult UpdateUserDirective([FromBody] UpdateUserDirectiveViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var userAccess = _userAccessService.GetUserAccess(model.UserId, model.AccessId);
            var directive = _directiveService.GetDirective(model.DirectiveId);
            if (userAccess == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId }); // Subject to change
            }
            else if (directive == null)
            {
                TempData["Error"] = "Заповедта не е намерена";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId }); // Subject to change
            }

            UserAccess ua = _userAccessService.UpdateUserAccessDirective(userAccess, directive);
            _logService.AddLog(loggedUser, LogAction.Edit, ua);

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult EditAccessName(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null) return NotFound();

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

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;
            List<UserAccess> userAccesses = new List<UserAccess>();

            var user = _userService.GetUser(model.FilterUserId);
            var access = _accessService.GetAccess(model.FilterAccessId);
            var directive = _directiveService.GetDirective(model.FilterDirectiveId);

            model.FilterUserName = user == null ? "" : user.UserName;
            model.FilterAccessDescription = access == null ? "" : _accessService.GetAccessDescription(access);
            model.FilterDirectiveDescription = directive == null ? "" : directive.Name;


            model.UserAccessList = _userAccessService.GetUserAccessesPaged(loggedUser, user, access, directive, page);

            return View(model);
        }

        [HttpPost]
        public IActionResult SoftDeleteAccess(Guid id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessToDelete = _accessService.GetAccess(id);
            if (accessToDelete == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("AccessList");
            }
            else if (!_accessService.CanDeleteAccess(accessToDelete))
            {
                TempData["Error"] = "Достъпът не може да бъде изтрит понеже той или някой от поддостъпите му е свързан с потребител!";
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

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.SuperAdmin;

            var result = new PagedResult<AccessListItemViewModel>
            {
                Items = _accessService.GetDeletedAccesses(page).Select(o => new AccessListItemViewModel
                {
                    AccessId = o.Id,
                    Description = _accessService.GetAccessDescription(o)
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

            var access = _accessService.GetDeletedAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("DeletedAccesses");
            }
            else if (!_accessService.CanRestoreAccess(access))
            {
                TempData["Error"] = "Достъпът не може да бъде възстановен защото липсва такъв родителски достъп";
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

            var access = _accessService.GetDeletedAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("DeletedAccesses");
            }

            _logService.AddLog(loggedUser, LogAction.HardDelete, access);
            _accessService.HardDeleteAccess(access);

            TempData["Success"] = "Достъпът е успешно изтрит.";
            return RedirectToAction("DeletedAccesses");
        }
    }
}
