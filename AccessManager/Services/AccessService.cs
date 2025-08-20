using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Utills;
using Microsoft.EntityFrameworkCore;

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
                    GrantedOn = DateTime.Now
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
                userAccess.RevokedOn = DateTime.Now;

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
                if (userAccess.RevokedByDirective != null) userAccess.RevokedByDirectiveId = directiveId;
                else userAccess.GrantedByDirectiveId = directiveId;

                _context.SaveChanges();
            }

            return userAccess;
        }

        internal int GetDeletedAccessesCount()
        {
            return _context.Accesses
                .IgnoreQueryFilters()
                .Count(a => a.DeletedOn != null);
        }

        internal IEnumerable<Access> GetDeletedAccesses(int page)
        {
            if (page < 1) page = 1;

            return _context.Accesses
                .IgnoreQueryFilters()
                .Where(a => a.DeletedOn != null)
                .OrderByDescending(a => a.DeletedOn)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();
        }

        internal Access GetDeletedAccess(Guid accessId)
        {
            return _context.Accesses.IgnoreQueryFilters()
                .Where(a => a.Id == accessId && a.DeletedOn != null)
                .First(a => a.Id == accessId);
        }
        internal bool CanRestoreAccess(Access access)
        {
            if (access.ParentAccessId == null)
                return true;

            var ancestorIds = GetParentAccessTree(access);

            return _context.Accesses
                               .IgnoreQueryFilters()
                               .Where(a => ancestorIds.Contains(a.Id))
                               .All(a => a.DeletedOn == null);
        }

        internal void RestoreAccess(Access access)
        {
            // We cannot simply restore the access because the parent id stays however the parent may be deleted
            // We will forbid restoring until all parents are restored in linear order (without the siblings)

            var accessIds = GetAccessSubTree(access);

            _context.Accesses.IgnoreQueryFilters()
                .Where(a => accessIds.Contains(a.Id))
                .ExecuteUpdate(a => a.SetProperty(x => x.DeletedOn, (DateTime?)null));
        }

        internal bool CanDeleteAccess(Access access)
        {
            var accessIds = GetAccessSubTree(access);
            return !_context.UserAccesses.Any(ua => accessIds.Contains(ua.AccessId));
        }

        internal void SoftDeleteAccess(Access access)
        {
            var timestamp = DateTime.Now;
            var accessIds = GetAccessSubTree(access);

            _context.Accesses
                .Where(a => accessIds.Contains(a.Id))
                .ExecuteUpdate(a => a.SetProperty(x => x.DeletedOn, timestamp));
        }

        internal void HardDeleteAccess(Access access)
        {
            var accessIds = GetAccessSubTree(access);

            _context.Accesses
                .IgnoreQueryFilters()
                .Where(a => accessIds.Contains(a.Id))
                .ExecuteDelete();
        }

        private List<Guid> GetAccessSubTree(Access access)
        {
            var accessIds = new List<Guid>();
            var stack = new Stack<Access>();
            stack.Push(access);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                accessIds.Add(current.Id);

                if (current.SubAccesses != null)
                    foreach (var sub in current.SubAccesses)
                        stack.Push(sub);
            }

            return accessIds;
        }

        private List<Guid> GetParentAccessTree(Access access)
        {
            var ancestors = new List<Guid>();
            if (access.ParentAccessId == null) return ancestors;

            var map = _context.Accesses
                .IgnoreQueryFilters()
                .Select(a => new { a.Id, a.ParentAccessId })
                .ToDictionary(a => a.Id, a => a.ParentAccessId);

            for (var parentId = access.ParentAccessId; parentId != null && map.ContainsKey(parentId.Value); parentId = map[parentId.Value])
                ancestors.Add(parentId.Value);

            return ancestors;
        }
    }
}
