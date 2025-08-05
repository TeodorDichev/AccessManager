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
            if(!_context.UserAccesses.Any(ua => ua.AccessId == userAccess.AccessId && ua.UserId == userAccess.UserId))
            {
                _context.UserAccesses.Add(userAccess);  
            }
        }
    }
}
