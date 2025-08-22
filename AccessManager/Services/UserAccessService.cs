using AccessManager.Data;
using AccessManager.Data.Entities;

namespace AccessManager.Services
{
    public class UserAccessService
    {
        private readonly Context _context;
        public UserAccessService(Context context)
        {
            _context = context;
        }

        internal UserAccess? GetUserAccess(Guid userId, Guid accessId)
        {
            return _context.UserAccesses.FirstOrDefault(ua => ua.AccessId == accessId && ua.UserId == userId);
        }

        internal List<UserAccess> GetGrantedUserAccesses(User user)
        {
            return _context.UserAccesses
                .Where(ua => ua.UserId == user.Id && ua.RevokedOn == null)
                .ToList();
        }

        internal List<UserAccess> GetGrantedUserAccesses(Access access)
        {
            return _context.UserAccesses
                .Where(ua => ua.Access.Id == access.Id && ua.RevokedOn == null)
                .ToList();
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

        internal UserAccess AddUserAccess(User user, Access access, Directive directive)
        {
            UserAccess? userAccess = _context.UserAccesses.FirstOrDefault(ua => ua.UserId == user.Id && ua.AccessId == access.Id);
            if (userAccess == null)
            {
                userAccess = new UserAccess
                {
                    Id = Guid.NewGuid(),
                    User = user,
                    UserId = user.Id,
                    AccessId = access.Id,
                    Access = access,
                    GrantedByDirectiveId = directive.Id,
                    GrantedByDirective = directive,
                    GrantedOn = DateTime.Now
                };

                _context.UserAccesses.Add(userAccess);
            }
            else
            {
                userAccess.RevokedOn = null;
                userAccess.RevokedByDirectiveId = null;
                userAccess.GrantedByDirectiveId = directive.Id;
                userAccess.GrantedByDirective = directive;
            }

            _context.SaveChanges();
            return userAccess;
        }

        internal UserAccess RevokeUserAccess(UserAccess userAccess, Directive directiveToRevokeAccess)
        {
            userAccess.RevokedByDirectiveId = directiveToRevokeAccess.Id;
            userAccess.RevokedByDirective = directiveToRevokeAccess;
            userAccess.RevokedOn = DateTime.Now;

            _context.SaveChanges();

            return userAccess;
        }

        internal UserAccess UpdateUserAccessDirective(UserAccess userAccess, Directive directive)
        {
            if (userAccess.RevokedByDirective != null)
            {
                userAccess.RevokedByDirective = directive;
                userAccess.RevokedByDirectiveId = directive.Id;
            }
            else
            {
                userAccess.GrantedByDirective = directive;
                userAccess.GrantedByDirectiveId = directive.Id;
            }

            _context.SaveChanges();

            return userAccess;
        }
    }
}
