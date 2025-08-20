using AccessManager.Data;
using AccessManager.Data.Entities;
using AccessManager.Utills;
using System.Text;

namespace AccessManager.Services
{
    public class FileService
    {
        private readonly Context _context;
        private readonly AccessService _accessService;
        public FileService(Context context, AccessService accessService)
        {
            _context = context;
            _accessService = accessService;
        }

        internal StringBuilder GetUsersCsv(List<User> accessibleUsers)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Средно име,Фамилия,Достъп за четене,Достъп за писане,Дирекция,Отдел,ЕГН,Телефон");

            foreach (var u in accessibleUsers.OrderBy(u => u.UserName))
            {
                sb.AppendLine($"\"{u.UserName}\",\"{u.FirstName}\",\"{u.MiddleName}\",\"{u.LastName}\",\"{BulgarianLocalization.GetBulgarianAuthorityType(u.ReadingAccess)}\"," +
                    $"\"{BulgarianLocalization.GetBulgarianAuthorityType(u.WritingAccess)}\",\"{u.Unit.Department.Description}\",\"{u.Unit.Description}\"," +
                    $"\"{u.EGN}\",\"{u.Phone}\"");
            }

            return sb;
        }

        internal StringBuilder GetUserAccessCsv()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Фамилия,Достъп,Заповед за даване,Спрян,Заповед за спиране");

            foreach (var u in _context.UserAccesses)
            {
                sb.AppendLine($"\"{u.User.UserName}\",\"{u.User.FirstName}\",\"{u.User.LastName}\",\"{_accessService.GetAccessDescription(u.Access)}\"," +
                    $"\"{u.GrantedByDirective.Name}\",\"{(u.RevokedByDirective == null ? "не" : "да")}\",\"{(u.RevokedByDirective == null ? "" : u.RevokedByDirective.Name)}\"");
            }

            return sb;
        }

        internal StringBuilder GetUnitsCsv(List<Unit> accessibleUnits)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Дирекция,Отдел към дирекцията");

            foreach (var u in accessibleUnits.OrderBy(u => u.Department.Description).ThenBy(u => u.Description))
            {
                sb.AppendLine($"\"{u.Department.Description}\",\"{u.Description}\"");
            }

            return sb;
        }

        internal StringBuilder GetAccessesCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Достъп");

            var accesses = _context.Accesses.OrderBy(d => d.Description).ToList();

            var childrenLookup = accesses.GroupBy(a => a.ParentAccessId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.ToList());

            void PrintAccess(Access access)
            {
                sb.AppendLine($"\"{_accessService.GetAccessDescription(access)}\"");

                if (childrenLookup.TryGetValue(access.Id, out var children))
                    foreach (var child in children)
                        PrintAccess(child);
            }

            if (childrenLookup.TryGetValue(Guid.Empty, out var roots))
                foreach (var root in roots)
                    PrintAccess(root);

            return sb;
        }

        internal StringBuilder GetUsersUnitsCsv(List<Unit> accessibleUnits)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Потребителско име,Собствено име,Фамилия,Достъп към отдел,Дирекция на отдела");

            foreach (var u in _context.UnitUsers.Where(u => accessibleUnits.Contains(u.Unit)).OrderBy(u => u.User.UserName).ThenBy(u => u.Unit.Department.Description).ThenBy(u => u.Unit.Description))
            {
                sb.AppendLine($"\"{u.User.UserName}\",\"{u.User.FirstName}\",\"{u.User.LastName}\",\"{u.Unit.Description}\",\"{u.Unit.Department.Description}\"");
            }

            return sb;
        }
    }
}
