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
                }).OrderBy(a =>a.Description).ToList();
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
    }
}
