using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Utills;
using AccessManager.ViewModels;
using AccessManager.ViewModels.Access;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.EntityFrameworkCore;

namespace AccessManager.Services
{
    public class UserAccessService
    {
        private readonly Context _context;
        private readonly AccessService _accessService;
        private readonly UserService _userService;
        public UserAccessService(Context context, AccessService accessService, UserService userService)
        {
            _context = context;
            _accessService = accessService;
            _userService = userService;
        }

        internal UserAccess? GetUserAccess(Guid userId, Guid accessId)
        {
            return _context.UserAccesses.FirstOrDefault(ua => ua.AccessId == accessId && ua.UserId == userId);
        }

        internal PagedResult<UserAccessListItemViewModel> GetUserAccessesPaged(
            User loggedUser, User? userFilter, Access? accessFilter, Directive? directiveFilter, int page)
        {
            var query = _context.UserAccesses
                .Include(ua => ua.User)                   
                    .ThenInclude(u => u.Position)         
                .Include(ua => ua.User)                  
                    .ThenInclude(u => u.Unit)            
                        .ThenInclude(unit => unit.Department)
                .Include(ua => ua.Access)
                .Include(ua => ua.GrantedByDirective)
                .Include(ua => ua.RevokedByDirective)
                .AsQueryable();

            if (userFilter != null)
                query = query.Where(ua => ua.UserId == userFilter.Id);
            else
            {
                var accessibleUserIds = _userService.GetAccessibleUsers(loggedUser).Select(u => u.Id).ToList();

                query = query.Where(ua => accessibleUserIds.Contains(ua.User.Id));
            }

            if (accessFilter != null)
                query = query.Where(ua => ua.AccessId == accessFilter.Id);

            if (directiveFilter != null)
                query = query.Where(ua =>
                    ua.GrantedByDirectiveId == directiveFilter.Id ||
                    ua.RevokedByDirectiveId == directiveFilter.Id);

            var totalCount = query.Count();

            var items = query
                .OrderBy(ua => ua.User.UserName)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(ua => new UserAccessListItemViewModel
                {
                    Id = ua.User.Id,
                    Position = ua.User.Position?.Description ?? "",
                    UserName = ua.User.UserName,
                    FirstName = ua.User.FirstName,
                    LastName = ua.User.LastName,
                    Department = ua.User.Unit.Department.Description,
                    Unit = ua.User.Unit.Description,
                    WriteAccess = ua.User.WritingAccess,
                    ReadAccess = ua.User.ReadingAccess,
                    AccessDescription = _accessService.GetAccessDescription(ua.Access),
                    GrantDirectiveDescription = ua.GrantedByDirective.Name,
                    RevokeDirectiveDescription = ua.RevokedByDirective != null ? ua.RevokedByDirective.Name : "-"
                })
                .ToList();

            return new PagedResult<UserAccessListItemViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
            };
        }

        internal PagedResult<UserAccessViewModel> GetUsersWithAccessPaged(User loggedUser, Access access, Directive? filterDirective, int page)
        {
            var accessibleUserIds = _userService.GetAccessibleUsers(loggedUser).Select(u => u.Id);

            var query = _context.UserAccesses
                .Include(ua => ua.User)
                    .ThenInclude(u => u.Position)
                .Include(ua => ua.User)
                    .ThenInclude(u => u.Unit)
                        .ThenInclude(unit => unit.Department)
                .Include(ua => ua.GrantedByDirective)
                .Where(ua => accessibleUserIds.Contains(ua.UserId))
                .Where(ua => ua.AccessId == access.Id && ua.RevokedOn == null);

            if (filterDirective != null)
                query = query.Where(ua => ua.GrantedByDirectiveId == filterDirective.Id);

            var totalCount = query.Count();

            var items = query
                .OrderBy(ua => ua.User.UserName)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .Select(ua => new UserAccessViewModel
                {
                    UserId = ua.UserId,
                    UserName = ua.User.UserName,
                    FirstName = ua.User.FirstName,
                    Position = ua.User.Position?.Description ?? "",
                    LastName = ua.User.LastName,
                    Department = ua.User.Unit.Department.Description,
                    Unit = ua.User.Unit.Description,
                    WriteAccess = ua.User.WritingAccess,
                    ReadAccess = ua.User.ReadingAccess,
                    DirectiveId = ua.GrantedByDirectiveId,
                    DirectiveDescription = ua.GrantedByDirective.Name
                })
                .ToList();

            return new PagedResult<UserAccessViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page
            };
        }

        internal PagedResult<UserAccessViewModel> GetUsersWithoutAccessPaged(User loggedUser, Access access, Directive? filterDirective, int page)
        {
            var revoked = GetRevokedUserAccesses(loggedUser, access)
                .Where(ua => filterDirective == null || ua.RevokedByDirectiveId == filterDirective.Id)
                .Select(ua => new UserAccessViewModel
                {
                    UserId = ua.UserId,
                    UserName = ua.User.UserName,
                    FirstName = ua.User.FirstName,
                    LastName = ua.User.LastName,
                    Department = ua.User.Unit.Department.Description,
                    Unit = ua.User.Unit.Description,
                    WriteAccess = ua.User.WritingAccess,
                    ReadAccess = ua.User.ReadingAccess,
                    DirectiveId = ua.RevokedByDirectiveId ?? Guid.Empty,
                    DirectiveDescription = ua.RevokedByDirective != null ? ua.RevokedByDirective.Name : "-"
                });

            // not granted users (only included if no directive filter applied)
            var notGranted = Enumerable.Empty<UserAccessViewModel>();
            if (filterDirective == null)
                notGranted = _accessService.GetNotGrantedUsers(loggedUser, access)
                    .Select(u => new UserAccessViewModel
                    {
                        UserId = u.Id,
                        UserName = u.UserName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Department = u.Unit.Department.Description,
                        Unit = u.Unit.Description,
                        WriteAccess = u.WritingAccess,
                        ReadAccess = u.ReadingAccess,
                        DirectiveId = Guid.Empty,
                        DirectiveDescription = ""
                    });

            var allWithoutAccess = revoked.Concat(notGranted).ToList();

            var totalCount = allWithoutAccess.Count;
            var items = allWithoutAccess
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * Constants.ItemsPerPage)
                .Take(Constants.ItemsPerPage)
                .ToList();

            return new PagedResult<UserAccessViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page
            };
        }

        internal List<UserAccess> GetRevokedUserAccesses(User loggedUser, Access access)
        {
            var accessibleUserIds = _userService.GetAccessibleUsers(loggedUser).Select(u => u.Id).ToList();

            return _context.UserAccesses
                .Where(ua => accessibleUserIds.Contains(ua.User.Id))
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
