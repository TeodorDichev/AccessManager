using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.Controllers
{
    public class AccessController : BaseController
    {
        private readonly UserService _userService;
        private readonly AccessService _accessService;
        private readonly DirectiveService _directiveService;

        public AccessController(Context context, UserService userService,
            AccessService accessService, DirectiveService directiveService)
        {
            _userService = userService;
            _accessService = accessService;
            _directiveService = directiveService;
        }

        [HttpGet]
        public IActionResult AccessList(int page = 1, int level = 0, Guid? filterAccessId = null)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var query = _accessService.GetAccesses().AsQueryable();

            if (level == 1)
            {
                query = query.Where(a => a.ParentAccessId == null);
            }
            else if (level >= 2 && filterAccessId.HasValue)
            {
                var allAccessesList = _accessService.GetAccesses().ToList();

                var lookup = allAccessesList
                    .Where(a => a.ParentAccessId.HasValue)
                    .GroupBy(a => a.ParentAccessId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var collectedIds = new HashSet<Guid>();
                void Collect(Guid parentId)
                {
                    if (!lookup.ContainsKey(parentId)) return;
                    foreach (var child in lookup[parentId])
                    {
                        if (collectedIds.Add(child.Id))
                            Collect(child.Id);
                    }
                }
                Collect(filterAccessId.Value);

                query = query.Where(a => collectedIds.Contains(a.Id));
            }

            var allAccesses = query
                .Select(a => new AccessListItemViewModel
                {
                    AccessId = a.Id,
                    Description = _accessService.GetAccessDescription(a),
                })
                .OrderBy(a => a.Description)
                .ToList();

            int total = allAccesses.Count;
            var accesses = allAccesses.Skip((page - 1) * Constants.ItemsPerPage)
                                      .Take(Constants.ItemsPerPage)
                                      .ToList();

            var model = new AccessListViewModel
            {
                Accesses = accesses,
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / Constants.ItemsPerPage),
                Level = level,
                FilterAccessId = filterAccessId,
            };

            if (filterAccessId.HasValue) {
                model.FilterDescription = _accessService.GetAccess(filterAccessId.Value.ToString()).Description;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteAccess(string id)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var accessToDelete = _accessService.GetAccess(id);
            if (accessToDelete == null) return NotFound();

            _accessService.SoftDeleteAccess(accessToDelete);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HardDeleteAccesses()
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            if (loggedUser.WritingAccess == AuthorityType.SuperAdmin) _accessService.HardDeleteAccesses();
            return RedirectToAction("AccessList");
        }

        [HttpGet]
        public IActionResult AccessUsersList(int page = 1)
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateAccess()
        {
            var model = new CreateAccessViewModel();
            return View("CreateAccess", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateAccessViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateAccess", model);
            }

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
                    if (!dict.TryGetValue(cur.ParentAccessId.Value, out cur)) { parentDepth = -1; break; }
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

            TempData["Success"] = "Достъпът е създаден успешно.";
            return RedirectToAction("AccessList"); // or whichever list action you have
        }

        [HttpGet]
        public IActionResult GetParentCandidates(int level = 1, string q = "")
        {
            if (level <= 0)
                return Json(new object[0]);

            var all = _accessService.GetAccesses()
                .Select(a => new { a.Id, a.Description, a.ParentAccessId })
                .ToList();

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
                .Select(a => new { id = a.Id, text = a.Description })
                .ToList();

            return Json(candidates);
        }

        [HttpGet]
        public IActionResult MapUserAccess(string username, string filterDirective1, string filterDirective2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(username);
            if (user == null) return NotFound();

            var directives = _directiveService.GetDirectives().Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToList();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var accessibleSystemsQuery = _accessService.GetGrantedUserAccesses(user).Select(ua => new AccessViewModel
            {
                AccessId = ua.Id,
                Description = _accessService.GetAccessDescription(ua.Access),
                DirectiveId = ua.GrantedByDirectiveId,
                DirectiveDescription = ua.GrantedByDirective.Name
            }).ToList();

            var notGrantedVm = _accessService
                .GetNotGrantedAccesses(user)
                .Select(a => new AccessViewModel
                {
                    AccessId = a.Id,
                    Description = _accessService.GetAccessDescription(a),
                    DirectiveId = Guid.Empty,
                    DirectiveDescription = ""
                });

            var revokedVm = _accessService
                .GetRevokedUserAccesses(user)
                .Select(ua => new AccessViewModel
                {
                    AccessId = ua.AccessId,
                    Description = _accessService.GetAccessDescription(ua.Access),
                    DirectiveId = ua.GrantedByDirectiveId,
                    DirectiveDescription = ua.RevokedByDirective != null ? ua.GrantedByDirective.Name : ""
                });

            var inaccessibleSystemsQuery =
                notGrantedVm
                    .Union(revokedVm)
                    .OrderBy(vm => vm.Description)
                    .ToList();

            if (!string.IsNullOrEmpty(filterDirective1))
                accessibleSystemsQuery = accessibleSystemsQuery.Where(u => u.DirectiveId == Guid.Parse(filterDirective1)).ToList();
            if (!string.IsNullOrEmpty(filterDirective2))
                inaccessibleSystemsQuery = inaccessibleSystemsQuery.Where(u => u.DirectiveId == Guid.Parse(filterDirective2)).ToList();

            var totalAccessible = accessibleSystemsQuery.Count();
            var totalInaccessible = inaccessibleSystemsQuery.Count();

            var accessibleSystems = accessibleSystemsQuery
                .OrderBy(u => u.Description)
                .Skip((page1 - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

            var inaccessibleSystems = inaccessibleSystemsQuery
                .OrderBy(u => u.Description)
                .Skip((page2 - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

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

        [HttpPost]
        public IActionResult UpdateAccess(string username, string? selectedAccessibleSystemIds, string? selectedInaccessibleSystemIds,
            string? directiveToRevokeAccess, string? directiveToGrantAccess)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var user = _userService.GetUser(username);
            if (user == null) return NotFound();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full || loggedUser.WritingAccess > user.WritingAccess;

            var removeIds = (selectedAccessibleSystemIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse)
                .ToList();

            var addIds = (selectedInaccessibleSystemIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse)
                .ToList();

            foreach (var accId in addIds)
                _accessService.AddUserAccess(user.Id, accId, directiveToGrantAccess);

            foreach (var accId in removeIds)
                _accessService.RevokeAccess(user.Id, accId, directiveToRevokeAccess);

            return RedirectToAction("MapUserAccess", new { username });
        }

        [HttpGet]
        public IActionResult EditAccess(Guid accessId, string? filterDirective1, string? filterDirective2, int page1 = 1, int page2 = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(accessId);
            if (access == null) return NotFound();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            var usersWithAccess = _accessService.GetGrantedUserAccesses(access)
                .Where(ua => ua.AccessId == accessId)
                .Select(ua => new UserAccessViewModel
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
                })
                .ToList();

            var usersWithRevokedAccess = _accessService.GetRevokedUserAccesses(access)
                .Where(ua => ua.AccessId == accessId)
                    .Select(ua => new UserAccessViewModel
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
                    })
                    .ToList();
            
            var usersNotGrantedTheAccess = _accessService.GetNotGrantedUsers(access)
                .Select(u => new UserAccessViewModel
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
                })
                .ToList();

            var usersWithoutAccess =
                usersWithRevokedAccess
                .Union(usersNotGrantedTheAccess)
                .OrderBy(vm => vm.UserName)
                .ToList();

            var model = new EditAccessViewModel
            {
                AccessId = accessId,
                Name = access.Description,
                Description = _accessService.GetAccessDescription(access),
                FilterDirectives = _directiveService.GetDirectives()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToList(),
                FilterDirective1 = filterDirective1,
                FilterDirective2 = filterDirective2,
                CurrentPage1 = page1,
                CurrentPage2 = page2,
                TotalPages1 = (int)Math.Ceiling(usersWithAccess.Count / (double)Constants.ItemsPerPage),
                TotalPages2 = (int)Math.Ceiling(usersWithoutAccess.Count / (double)Constants.ItemsPerPage),
                UsersWithAccess = usersWithAccess
                    .OrderBy(vm => vm.UserName)
                    .Skip((page1 - 1) * Constants.ItemsPerPage)
                    .Take(Constants.ItemsPerPage)
                    .ToList(),
                UsersWithoutAccess = usersWithoutAccess
                    .OrderBy(vm => vm.UserName)
                    .Skip((page2 - 1) * Constants.ItemsPerPage)
                    .Take(Constants.ItemsPerPage)
                    .ToList(),
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult GrantAccessToUsers(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null) return NotFound();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            if(string.IsNullOrEmpty(model.DirectiveToGrantAccess))
            {
                TempData["ErrorMessage"] = "Please select a directive before adding access!";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithoutAccessIds)
                _accessService.AddUserAccess(userId, access.Id, model.DirectiveToGrantAccess);

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult RevokeAccessFromUsers(EditAccessViewModel model)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            var access = _accessService.GetAccess(model.AccessId);
            if (access == null) return NotFound();

            ViewBag.IsReadOnly = loggedUser.WritingAccess < Data.Enums.AuthorityType.Full;

            if (string.IsNullOrEmpty(model.DirectiveToRevokeAccess))
            {
                TempData["ErrorMessage"] = "Please select a directive before revoking access!";
                return RedirectToAction("EditAccess", new { accessId = model.AccessId });
            }

            foreach (var userId in model.SelectedUsersWithAccessIds)
                _accessService.RevokeAccess(userId, access.Id, model.DirectiveToRevokeAccess);

            return RedirectToAction("EditAccess", new { accessId = model.AccessId });
        }

        [HttpPost]
        public IActionResult UpdateUserDirective([FromBody] UpdateUserDirectiveViewModel dto)
        {
            // TODO: Update directive for user-access mapping
            return Ok();
        }
    }
}
