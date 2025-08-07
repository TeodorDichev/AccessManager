using AccessManager.Data;
using AccessManager.Data.Entities;
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

        internal List<AccessViewModel> GetUserAccesses(User loggedUser)
        {
            return _context.UserAccesses
                .Where(ua => ua.UserId == loggedUser.Id && ua.RevokedOn == null)
                .ToList()
                .Select(ua => new AccessViewModel
                {
                    Id = ua.Id,
                    Description = GetAccessDescription(ua.Access),
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
    }
}
