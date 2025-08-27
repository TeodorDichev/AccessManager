using AccessManager.Data.Enums;

namespace AccessManager.Utills
{
    public static class BulgarianLocalization
    {
        public static string GetBulgarianAuthorityType(AuthorityType access)
        {
            return access switch
            {
                AuthorityType.SuperAdmin => "Главен администратор",
                AuthorityType.Full => "Пълен",
                AuthorityType.Restricted => "Частичен",
                AuthorityType.None => "Няма",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetBulgarianLogAction(LogAction logAction)
        {
            return logAction switch
            {
                LogAction.Add => "създаде",
                LogAction.Delete => "деактивира",
                LogAction.Edit => "редактира",
                LogAction.Restore => "възстанови",
                LogAction.HardDelete => "изтри",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetBulgarianSort(UserSortOptions sort)
        {
            return sort switch
            {
                UserSortOptions.Username => "потребителско име",
                UserSortOptions.FirstName => "собствено име",
                UserSortOptions.LastName => "фамилия",
                UserSortOptions.ReadingAccess => "достъп за четене",
                UserSortOptions.WritingAccess => "достъп за писане",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
