using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Data.Enums;
using AccessManager.Services;
using AccessManager.Utills;
using AccessManager.ViewModels.Access;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult AccessList(int page = 1)
        {
            var loggedUser = _userService.GetUser(HttpContext.Session.GetString("Username"));
            if (loggedUser == null) return RedirectToAction("Login", "Home");

            List<AccessListItemViewModel> allAccesses = _accessService.GetAccesses().Select(a => new AccessListItemViewModel
            {
                AccessId = a.Id,
                Description = _accessService.GetAccessDescription(a),
            }).OrderBy(a => a.Description).ToList();
            ;
            int totalUsers = allAccesses.Count();

            List<AccessListItemViewModel> accesses = allAccesses.Skip((page - 1) * Constants.ItemsPerPage).Take(Constants.ItemsPerPage).ToList();

            var model = new AccessListViewModel
            {
                Accesses = accesses,
                WriteAuthority = loggedUser.WritingAccess,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / Constants.ItemsPerPage)
            };

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

            // validation: parent required for level > 0
            if (model.Level > 0 && !model.ParentAccessId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ParentAccessId), "Моля изберете родителски достъп за това ниво.");
                return View("CreateAccess", model);
            }

            // if parent specified ensure its depth == model.Level - 1
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

        // Autocomplete / candidates endpoint
        [HttpGet]
        public IActionResult GetParentCandidates(int level = 1, string q = "")
        {
            if (level <= 0)
                return Json(new object[0]);

            // load all active accesses (small admin set usually)
            var all = _accessService.GetAccesses()
                .Select(a => new { a.Id, a.Description, a.ParentAccessId })
                .ToList();

            // build lookup for parent traversal
            var dict = all.ToDictionary(x => x.Id, x => x);

            // compute depth for each access (distance from root)
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
                        depth = -1; // broken parent chain
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
                .Take(30) // cap
                .Select(a => new { id = a.Id, text = a.Description })
                .ToList();

            return Json(candidates);
        }
    }
}
