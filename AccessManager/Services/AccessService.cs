using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.Services
{
    public class AccessService
    {
        private readonly Context _context;
        public AccessService(Context context)
        {
            _context = context;
        }

        internal List<AccessViewModel> GetGrantedUserAccesses(User loggedUser)
        {
            return _context.UserAccesses
                .Where(ua => ua.UserId == loggedUser.Id && ua.RevokedOn == null)
                .ToList()
                .Select(ua => new AccessViewModel
                {
                    AccessId = ua.Id,
                    Description = GetAccessDescription(ua.Access),
                    DirectiveId = ua.GrantedByDirectiveId,
                    DirectiveDescription = ua.GrantedByDirective.Name
                }).ToList();
        }

        internal string GetAccessDescription(Access access)
        {
            var result = access.Description;
            var current = access.ParentAccess;
            while (current != null)
            {
                result = current.Description + " -> " + result;
                current = current.ParentAccess;
            }
            return result;
        }

        internal List<string> GetDirectivesDescription()
        {
            return _context.Directives.Select(d => d.Name).Distinct().ToList();
        }

        internal List<AccessListItemViewModel> GetAccesses()
        {
            return _context.Accesses.Where(a => a.DeletedOn == null).ToList()
                .Select(a => new AccessListItemViewModel
                {
                    AccessId = a.Id,
                    Description = GetAccessDescription(a),
                }).OrderBy(a => a.Description).ToList();
        }

        internal Access? GetAccess(string id)
        {
            return _context.Accesses.FirstOrDefault(a => a.Id == Guid.Parse(id) && a.DeletedOn == null);
        }

        internal void SoftDeleteAccess(Access accessToDelete)
        {
            accessToDelete.DeletedOn = DateTime.UtcNow;
            SoftDeleteRecursively(accessToDelete, DateTime.UtcNow);
            _context.SaveChanges();
        }

        internal void HardDeleteAccesses()
        {
            _context.Accesses.Where(a => a.DeletedOn != null).ToList().ForEach(a => _context.Accesses.Remove(a));
            _context.SaveChanges();
        }
        private void SoftDeleteRecursively(Access access, DateTime timestamp)
        {
            if (access == null) return;

            access.DeletedOn = timestamp;

            if (access.SubAccesses != null && access.SubAccesses.Any())
            {
                foreach (var subAccess in access.SubAccesses)
                {
                    SoftDeleteRecursively(subAccess, timestamp);
                }
            }
        }

        internal UserAccess? GetUserAccess(string v, string username)
        {
            return _context.UserAccesses.FirstOrDefault(ua => ua.Id == Guid.Parse(v) && ua.User.UserName == username);
        }

        internal void UpdateAccessDirective(UserAccess access, string directiveId)
        {
            if (_context.Directives.Any(d => d.Id == Guid.Parse(directiveId)))
                access.GrantedByDirectiveId = Guid.Parse(directiveId);

        }

        internal List<SelectListItem> GetDirectives()
        {
            return _context.Directives
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToList();
        }

        internal List<AccessViewModel> GetRevokedAndNotGrantedAccesses(User user)
        {
            var userId = user.Id;

            var revokedQuery =
                from ua in _context.UserAccesses
                where ua.UserId == userId && ua.RevokedOn != null
                select new AccessViewModel
                {
                    AccessId = ua.AccessId,
                    Description = GetAccessDescription(ua.Access),
                    DirectiveId = ua.GrantedByDirectiveId,
                    DirectiveDescription = ua.GrantedByDirective.Name
                };

            var neverGrantedQuery =
                from a in _context.Accesses
                where a.DeletedOn == null
                   && !_context.UserAccesses.Any(ua => ua.UserId == userId && ua.AccessId == a.Id)
                select new AccessViewModel
                {
                    AccessId = a.Id,
                    Description = GetAccessDescription(a),
                    DirectiveId = Guid.Empty,
                    DirectiveDescription = string.Empty
                };

            var result = revokedQuery
                .Union(neverGrantedQuery)
                .ToList();

            return result.OrderBy(av => av.Description).ToList();
        }

        internal bool ExistsDirectiveWithId(string directiveToRevokeAccess)
        {
            return _context.Directives.Any(d => d.Id == Guid.Parse(directiveToRevokeAccess));
        }

        internal void AddAccess(Guid userId, List<Guid> addIds, string directiveToGrantAccess)
        {
            foreach (var id in addIds)
            {
                var access = _context.Accesses.FirstOrDefault(a => a.Id == id && a.DeletedOn == null);
                if (access != null)
                {
                    var userAccess = new UserAccess
                    {
                        UserId = userId,
                        AccessId = access.Id,
                        GrantedByDirectiveId = Guid.Parse(directiveToGrantAccess),
                        GrantedOn = DateTime.UtcNow
                    };
                    _context.UserAccesses.Add(userAccess);
                }
            }
            _context.SaveChanges();
        }

        internal void RevokeAccess(Guid userId, List<Guid> removeIds, string directiveToRevokeAccess)
        {
            foreach (var id in removeIds)
            {
                var userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == id && ua.DeletedOn == null);
                if (userAccess != null)
                {
                    userAccess.RevokedByDirectiveId = Guid.Parse(directiveToRevokeAccess);
                    userAccess.RevokedOn = DateTime.UtcNow;
                }
            }
            _context.SaveChanges();
        }
    }
}
