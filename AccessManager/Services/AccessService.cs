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

        internal Access? GetAccess(Guid id)
        {
            return _context.Accesses.FirstOrDefault(a => a.Id == id);
        }
        internal void AddAccess(Access acc)
        {
            _context.Accesses.Add(acc);
            _context.SaveChanges();
        }

        internal void UpdateAccessName(string name, Access access)
        {
            access.Description = name;
            _context.SaveChanges();
        }

        internal string GetAccessDescription(Access access)
        {
            if (access == null)
                return string.Empty;

            var accessMap = _context.Accesses
                .IgnoreQueryFilters()
                .Select(a => new { a.Id, a.ParentAccessId, a.Description })
                .ToDictionary(a => a.Id, a => new { a.ParentAccessId, a.Description });

            var descriptions = new List<string>();
            var currentParentId = access.ParentAccessId;

            while (currentParentId != null && accessMap.ContainsKey(currentParentId.Value))
            {
                var parent = accessMap[currentParentId.Value];
                descriptions.Add(parent.Description);
                currentParentId = parent.ParentAccessId;
            }

            descriptions.Reverse();
            descriptions.Add(access.Description);

            return string.Join(" -> ", descriptions);
        }
        internal List<Access> GetAccesses()
        {
            return _context.Accesses.ToList();
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
