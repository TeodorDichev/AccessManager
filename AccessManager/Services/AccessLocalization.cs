using AccessManager.Data.Enums;

namespace AccessManager.Services
{
    public static class AccessLocalization
    {
        public static string GetBulgarianReadingAccess(ReadingAccess access)
        {
            return access switch
            {
                ReadingAccess.Full => "Пълен",
                ReadingAccess.Partial => "Частичен",
                ReadingAccess.None => "Няма",
                _ => "Неопределен",
            };
        }

        public static string GetBulgarianWritingAccess(WritingAccess access)
        {
            return access switch
            {
                WritingAccess.Full => "Пълен",
                WritingAccess.Partial => "Частичен",
                WritingAccess.None => "Няма",
                _ => "Неопределен",
            };
        }
    }
}
