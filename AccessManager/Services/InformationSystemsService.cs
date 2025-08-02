using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.ViewModels.InformationSystem;

namespace AccessManager.Services
{
    public class InformationSystemsService
    {
        private readonly Context _context;
        public InformationSystemsService(Context context)
        {
            _context = context;
        }

        public List<InformationSystem> GetAllInformationSystems()
        {
            return _context.InformationSystems.Where(s => s.DeletedOn == null).ToList();
        }

        internal void AddAccesses(Guid userId, List<AccessViewModel> accesses)
        {
            foreach (var acc in accesses)
            {
                bool exists = _context.UserAccesses.Any(ua => ua.UserId == userId && ua.AccessId == acc.Id);
                if (!exists)
                {
                    _context.UserAccesses.Add(new UserAccess
                    {
                        UserId = userId,
                        AccessId = acc.Id,
                        Directive = acc.Directive ?? string.Empty,
                        GrantedOn = DateTime.Now
                    });
                }
            }

            _context.SaveChanges();
        }

        internal void RemoveUserAccess(Guid userId, Guid accessId)
        {
            var userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == accessId);

            if (userAccess != null)
            {
                _context.UserAccesses.Remove(userAccess);
                _context.SaveChanges();
            }
        }

        internal void UpdateAccessDirective(Guid userId, Guid accessId, string directive)
        {
            var userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == accessId);
            if (userAccess != null)
            {
                userAccess.Directive = directive;
                _context.SaveChanges();
            }
        }
    }
}
