using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly LogService _logService;
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;

        public AccessController(Context context, UserService userService, LogService logService,
            AccessService accessService, DirectiveService directiveService)
        {
            _logService = logService;
            _userService = userService;
            _accessService = accessService;
            _directiveService = directiveService;
        }

        [HttpGet]
        public IActionResult AccessList(int page = 1, int level = 0, Guid? filterAccessId = null)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var query = _accessService.GetAccesses().AsQueryable();

            if (level == 1) query = query.Where(a => a.ParentAccessId == null);
            else if (level >= 2 && filterAccessId.HasValue)
            {
                var allAccessesList = _accessService.GetAccesses().ToList();

                var lookup = allAccessesList.Where(a => a.ParentAccessId.HasValue).GroupBy(a => a.ParentAccessId.Value).ToDictionary(g => g.Key, g => g.ToList());

                var collectedIds = new HashSet<Guid>();
                void Collect(Guid parentId)
                {
                    if (!lookup.ContainsKey(parentId)) return;
                    foreach (var child in lookup[parentId])
                        if (collectedIds.Add(child.Id))
                            Collect(child.Id);
                }
                Collect(filterAccessId.Value);

                query = query.Where(a => collectedIds.Contains(a.Id));
            }

            var allAccesses = query.Select(a => new AccessListItemViewModel
            {
                AccessId = a.Id,
                Description = _accessService.GetAccessDescription(a),
            }).OrderBy(a => a.Description).ToList();

            int total = allAccesses.Count;

            var model = new AccessListViewModel
            {
                Accesses = allAccesses.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList(),
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / Constants.ItemsPerPage),
                Level = level,
                FilterAccessId = filterAccessId,
            };

            if (filterAccessId.HasValue)
                model.FilterDescription = _accessService.GetAccess(filterAccessId.Value.ToString()).Description;

            return View(model);
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
        public IActionResult GetAccesses(string q = "")
        {
            var all = _accessService.GetAccesses().Select(a => new { a.Id, a.Description }).ToList();
            var qLower = (q ?? "").Trim().ToLowerInvariant();

            var candidates = all
                .Where(a => string.IsNullOrEmpty(qLower) || a.Description.ToLowerInvariant().Contains(qLower))
                .OrderBy(a => a.Description)
                .Take(30)
                .Select(a => new { id = a.Id, text = _accessService.GetAccessDescription(_accessService.GetAccess(a.Id)) })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult MapUserAccess(string username, string filterDirective1, string filterDirective2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var user = _userService.GetUser(username);
            if (user == null)
            {
                TempData["Error"] = "Потребителят не е намерен";
                return RedirectToAction("EditUser", "User", new { username });
            }

            var directives = _directiveService.GetDirectives().Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToList();

            var accessibleSystemsQuery = _accessService.GetGrantedUserAccesses(user).Select(ua => new AccessViewModel
            {
                AccessId = ua.Access.Id,
                Description = _accessService.GetAccessDescription(ua.Access),
                DirectiveId = ua.GrantedByDirectiveId,
                DirectiveDescription = ua.GrantedByDirective.Name
            }).ToList();

            var notGrantedVm = _accessService.GetNotGrantedAccesses(user).Select(a => new AccessViewModel
            {
                AccessId = a.Id,
                Description = _accessService.GetAccessDescription(a),
                DirectiveId = Guid.Empty,
                DirectiveDescription = ""
            });

            var revokedVm = _accessService.GetRevokedUserAccesses(user).Select(ua => new AccessViewModel
            {
                AccessId = ua.AccessId,
                Description = _accessService.GetAccessDescription(ua.Access),
                DirectiveId = ua.GrantedByDirectiveId,
                DirectiveDescription = ua.RevokedByDirective != null ? ua.GrantedByDirective.Name : ""
            });

            var inaccessibleSystemsQuery = notGrantedVm.Union(revokedVm).OrderBy(vm => vm.Description).ToList();

            if (!string.IsNullOrEmpty(filterDirective1))
                accessibleSystemsQuery = accessibleSystemsQuery.Where(u => u.DirectiveId == Guid.Parse(filterDirective1)).ToList();
            if (!string.IsNullOrEmpty(filterDirective2))
                inaccessibleSystemsQuery = inaccessibleSystemsQuery.Where(u => u.DirectiveId == Guid.Parse(filterDirective2)).ToList();

            var totalAccessible = accessibleSystemsQuery.Count();
            var totalInaccessible = inaccessibleSystemsQuery.Count();

            var accessibleSystems = accessibleSystemsQuery.OrderBy(u => u.Description).Skip((page1 - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();
            var inaccessibleSystems = inaccessibleSystemsQuery.OrderBy(u => u.Description).Skip((page2 - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var vm = new MapUserAccessViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Department = user.Unit.Department.Description,
                Unit = user.Unit.Description,
                FilterDirectives = directives,
                FilterDirective1 = filterDirective1,
                FilterDirective2 = filterDirective2,
                AccessibleSystems = accessibleSystems,
                InaccessibleSystems = inaccessibleSystems,
                CurrentPage1 = page1,
                TotalPages1 = (int)Math.Ceiling(totalAccessible / (double)Constants.ItemsPerPage),
                CurrentPage2 = page2,
                TotalPages2 = (int)Math.Ceiling(totalInaccessible / (double)Constants.ItemsPerPage)
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult EditAccess(Guid accessId, string? filterDirective1, string? filterDirective2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(accessId);
            if (access == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("AccessList");
            }

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var usersWithAccess = _accessService.GetGrantedUserAccesses(access).Select(ua => new UserAccessViewModel
            {
                UserId = ua.UserId,
                UserName = ua.User.UserName,
                FirstName = ua.User.FirstName,
                LastName = ua.User.LastName,
                Department = ua.User.Unit.Department.Description,
                Unit = ua.User.Unit.Description,
                WriteAccess = ua.User.WritingAccess,
                ReadAccess = ua.User.ReadingAccess,
                DirectiveId = ua.GrantedByDirectiveId,
                DirectiveDescription = ua.GrantedByDirective.Name
            }).ToList();

            var usersWithRevokedAccess = _accessService.GetRevokedUserAccesses(access).Select(ua => new UserAccessViewModel
            {
                UserId = ua.UserId,
                UserName = ua.User?.UserName ?? "",
                FirstName = ua.User?.FirstName ?? "",
                LastName = ua.User?.LastName ?? "",
                Department = ua.User?.Unit?.Department?.Description ?? "",
                Unit = ua.User?.Unit?.Description ?? "",
                WriteAccess = ua.User?.WritingAccess ?? AuthorityType.None,
                ReadAccess = ua.User?.ReadingAccess ?? AuthorityType.None,
                DirectiveId = ua.RevokedByDirectiveId ?? Guid.Empty,
                DirectiveDescription = ua.RevokedByDirective?.Name ?? ""
            }).ToList();

            var usersNotGrantedTheAccess = _accessService.GetNotGrantedUsers(access).Select(u => new UserAccessViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Department = u.Unit.Department.Description,
                Unit = u.Unit.Description,
                WriteAccess = u.WritingAccess,
                ReadAccess = u.ReadingAccess,
                DirectiveId = Guid.Empty,
                DirectiveDescription = ""
            }).ToList();

            var usersWithoutAccess = usersWithRevokedAccess.Union(usersNotGrantedTheAccess).OrderBy(vm => vm.UserName).ToList();

            var model = new EditAccessViewModel
            {
                AccessId = accessId,
                Name = access.Description,
                Description = _accessService.GetAccessDescription(access),
                FilterDirectives = _directiveService.GetDirectives()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToList(),
                FilterDirective1 = filterDirective1 ?? "",
                FilterDirective2 = filterDirective2 ?? "",
                CurrentPage1 = page1,
                CurrentPage2 = page2,
                TotalPages1 = (int)Math.Ceiling(usersWithAccess.Count / (double)Constants.ItemsPerPage),
                TotalPages2 = (int)Math.Ceiling(usersWithoutAccess.Count / (double)Constants.ItemsPerPage),
                UsersWithAccess = usersWithAccess.OrderBy(vm => vm.UserName).Skip((page1 - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList(),
                UsersWithoutAccess = usersWithoutAccess.OrderBy(vm => vm.UserName).Skip((page2 - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList(),
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult GrantAccessToUsers(MapUserAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserName);
            if (user == null || string.IsNullOrEmpty(model.DirectiveToGrantAccess))
            {
                TempData["Error"] = "Please select a directive before adding access!";
                return RedirectToAction("EditAccess", new { username = model.UserName });
            }

            foreach (var accId in model.SelectedInaccessibleSystemIds)
            {
                UserAccess ua = _accessService.AddUserAccess(user.Id, accId, model.DirectiveToGrantAccess);
                _logService.AddLog(loggedUser, LogAction.Add, ua);
            }

            return RedirectToAction("MapUserAccess", new { username = model.UserName });
        }

        [HttpPost]
        public IActionResult RevokeAccessFromUsers(MapUserAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(model.UserName);
            if (user == null || string.IsNullOrEmpty(model.DirectiveToRevokeAccess))
            {
                TempData["Error"] = "Please select a directive before revoking access!";
                return RedirectToAction("MapUserAccess", new { username = model.UserName });
            }

            foreach (var accId in model.SelectedAccessibleSystemIds)
            {
                UserAccess ua = _accessService.RevokeAccess(user.Id, accId, model.DirectiveToRevokeAccess);
                _logService.AddLog(loggedUser, LogAction.Add, ua);
            }

            return RedirectToAction("MapUserAccess", new { username = model.UserName });
        }

        [HttpPost]
        public IActionResult GrantUserAccess(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null || string.IsNullOrEmpty(model.DirectiveToGrantAccess))
            {
                TempData["Error"] = "Please select a directive before adding access!";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithoutAccessIds)
            {
                UserAccess ua = _accessService.AddUserAccess(userId, access.Id, model.DirectiveToGrantAccess);
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
            if (access == null || string.IsNullOrEmpty(model.DirectiveToRevokeAccess))
            {
                TempData["Error"] = "Please select a directive before revoking access!";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithAccessIds)
            {
                UserAccess ua = _accessService.RevokeAccess(userId, access.Id, model.DirectiveToRevokeAccess);
                _logService.AddLog(loggedUser, LogAction.Add, ua);
            }

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult UpdateUserDirective([FromBody] UpdateUserDirectiveViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null) return NotFound();

            if (model.UserId == Guid.Empty)
            {
                model.UserId = _userService.GetUser(model.username)?.Id ?? Guid.Empty;
            }

            UserAccess ua = _accessService.UpdateAccessDirective(model.UserId, access.Id, model.DirectiveId);
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

            if (model.FilterUserId.HasValue)
            {
                var user = _userService.GetUser(model.FilterUserId.Value);
                if (user != null)
                {
                    model.FilterUserName = user.UserName;
                    userAccesses = user.UserAccesses.ToList();
                }
            }
            else
                foreach (var user in _userService.GetAccessibleUsers(loggedUser).Append(loggedUser).ToList())
                    userAccesses.AddRange(user.UserAccesses.ToList());

            if (model.FilterAccessId.HasValue)
            {
                var access = _accessService.GetAccess(model.FilterAccessId.Value);
                if (access != null)
                {
                    model.FilterAccessDescription = _accessService.GetAccessDescription(access);
                    userAccesses = userAccesses.Where(ua => ua.AccessId == model.FilterAccessId).ToList();
                }
            }

            if (model.FilterDirectiveId.HasValue)
            {
                var directive = _directiveService.GetDirective(model.FilterDirectiveId.Value);
                if (directive != null)
                {
                    model.FilterDirectiveDescription = directive.Name;
                    userAccesses = userAccesses.Where(ua => ua.GrantedByDirectiveId == model.FilterDirectiveId || ua.RevokedByDirectiveId == model.FilterDirectiveId).ToList();
                }
            }

            model.UserAccessList = userAccesses.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).Select(ua =>
                new UserAccessListItemViewModel
                {
                    UserName = ua.User.UserName,
                    FirstName = ua.User.FirstName,
                    LastName = ua.User.LastName,
                    Department = ua.User.Unit.Department.Description,
                    Unit = ua.User.Unit.Description,
                    WriteAccess = ua.User.WritingAccess,
                    ReadAccess = ua.User.ReadingAccess,
                    AccessDescription = _accessService.GetAccessDescription(ua.Access),
                    GrantDirectiveDescription = ua.GrantedByDirective.Name,
                    RevokeDirectiveDescription = ua.RevokedByDirective != null ? ua.RevokedByDirective.Name : "-",
                }).OrderBy(u => u.UserName).ToList();

            model.CurrentPage = page;
            model.TotalPages = (int)Math.Ceiling(userAccesses.Count / (double)Constants.ItemsPerPage);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteAccess(string id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessToDelete = _accessService.GetAccess(id);
            if (accessToDelete == null)
            {
                TempData["Error"] = "Достъпът не е намерен";
                return RedirectToAction("AccessList");

            }
            else if(!_accessService.CanDeleteAccess(accessToDelete))
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

            var model = new DeletedAccessesViewModel()
            {
                Accesses = _accessService.GetDeletedAccesses(page)
                    .Select(a => new AccessListItemViewModel
                    {
                        AccessId = a.Id,
                        Description = _accessService.GetAccessDescription(a),
                    })
                    .ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)_accessService.GetDeletedAccessesCount()/Constants.ItemsPerPage)
            };
            return View(model);
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
            else if(!_accessService.CanRestoreAccess(access))
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
