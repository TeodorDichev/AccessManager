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

        internal List<UserAccess> GetGrantedUserAccesses(User loggedUser)
        {
            return _context.UserAccesses
                .Where(ua => ua.UserId == loggedUser.Id && ua.RevokedOn == null)
                .ToList();
        }

        internal List<UserAccess> GetGrantedUserAccesses(Access access)
        {
            return _context.UserAccesses
                .Where(ua => ua.Access.Id == access.Id && ua.RevokedOn == null)
                .ToList();
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

        internal List<Access> GetAccesses()
        {
            return _context.Accesses.Where(a => a.DeletedOn == null).ToList();
        }

        internal Access? GetAccess(string id)
        {
            return GetAccess(Guid.Parse(id));
        }
        internal Access? GetAccess(Guid id)
        {
            return _context.Accesses.FirstOrDefault(a => a.Id == id);
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

        internal List<UserAccess> GetRevokedUserAccesses(User user)
        {
            return _context.UserAccesses
                .Where(ua => ua.UserId == user.Id && ua.RevokedOn != null)
                .ToList();
        }

        internal List<UserAccess> GetRevokedUserAccesses(Access access)
        {
            return _context.UserAccesses
                .Where(ua => ua.AccessId == access.Id && ua.RevokedOn != null)
                .ToList();
        }

        internal List<Access> GetNotGrantedAccesses(User user)
        {
            return _context.Accesses
                .Where(a => !_context.UserAccesses.Any(ua => ua.UserId == user.Id && ua.AccessId == a.Id))
                .ToList();
        }
        internal List<User> GetNotGrantedUsers(Access access)
        {
            return _context.Users
                .Where(u => !_context.UserAccesses.Any(ua => ua.UserId == u.Id && ua.AccessId == access.Id))
                .ToList();
        }

        internal bool ExistsDirectiveWithId(string directiveToRevokeAccess)
        {
            return _context.Directives.Any(d => d.Id == Guid.Parse(directiveToRevokeAccess));
        }

        internal UserAccess AddUserAccess(Guid userId, Guid accessId, string directiveToGrantAccess)
        {
            UserAccess? userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == accessId);
            if (userAccess == null)
            {
                userAccess = new UserAccess
                {
                    UserId = userId,
                    AccessId = accessId,
                    GrantedByDirectiveId = Guid.Parse(directiveToGrantAccess),
                    GrantedOn = DateTime.UtcNow
                };

                _context.UserAccesses.Add(userAccess);
            }
            else
            {
                userAccess.RevokedOn = null;
                userAccess.RevokedByDirectiveId = null;
                userAccess.GrantedByDirectiveId = Guid.Parse(directiveToGrantAccess);
            }

            _context.SaveChanges();
            return userAccess;
        }

        internal void AddAccess(Access acc)
        {
            _context.Accesses.Add(acc);
            _context.SaveChanges();
        }

        internal UserAccess RevokeAccess(Guid userId, Guid accessId, string directiveToRevokeAccess)
        {
            UserAccess? userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == accessId);

            if (userAccess != null)
            {
                userAccess.RevokedByDirectiveId = Guid.Parse(directiveToRevokeAccess);
                userAccess.RevokedOn = DateTime.UtcNow;

                _context.SaveChanges();
            }

            return userAccess;
        }

        internal void UpdateAccessName(string name, Access access)
        {
            access.Description = name;
            _context.SaveChanges();
        }

        internal UserAccess UpdateAccessDirective(Guid userId, Guid accessId, Guid directiveId)
        {
            UserAccess? userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == userId && ua.AccessId == accessId);
            if (userAccess != null)
            {
                if(userAccess.RevokedByDirective != null) userAccess.RevokedByDirectiveId = directiveId;
                else userAccess.GrantedByDirectiveId = directiveId;

                _context.SaveChanges();
            }

            return userAccess;
        }
    }
}
