using AccessManager.Data.Enums;

namespace AccessManager.Services
{
    public static class AuthorityTypeLocalization
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
    }
}
