using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class AccessService
    {
        private readonly Context _context;
        public AccessService(Context context)
        {
            _context = context;
        }

        public void AddUserAccess(UserAccess userAccess)
        {
            _context.UserAccesses.Add(userAccess);
        }
    }
}
