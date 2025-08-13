using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;

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

        internal List<AccessViewModel> GetRevokedAndNotGrantedAccesses(User user)
        {
            var userId = user.Id;

            var revokedQuery =
                from ua in _context.UserAccesses
                where ua.UserId == userId && ua.RevokedOn != null
                select new
                {
                    AccessId = ua.AccessId,
                    Description = ua.Access.Description,
                    DirectiveId = (Guid?)ua.GrantedByDirectiveId,
                    DirectiveDescription = ua.GrantedByDirective.Name
                };

            var neverGrantedQuery =
                from a in _context.Accesses
                where a.DeletedOn == null
                   && !_context.UserAccesses.Any(ua => ua.UserId == userId && ua.AccessId == a.Id)
                select new
                {
                    AccessId = a.Id,
                    Description = a.Description,
                    DirectiveId = (Guid?)null,
                    DirectiveDescription = (string)null
                };

            var result = revokedQuery
                .Union(neverGrantedQuery)
                .AsEnumerable() // switch to in-memory so we can call C# methods
                .Select(x => new AccessViewModel
                {
                    AccessId = x.AccessId,
                    Description = GetAccessDescription(GetAccess(x.AccessId.ToString())), // now safe
                    DirectiveId = x.DirectiveId ?? Guid.Empty,
                    DirectiveDescription = x.DirectiveDescription ?? string.Empty
                })
                .OrderBy(av => av.Description)
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
