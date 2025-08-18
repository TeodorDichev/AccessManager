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
                LogAction.Delete => "премахна",
                LogAction.Edit => "редактира",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
