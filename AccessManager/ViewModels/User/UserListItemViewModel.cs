using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.User
{
    public class UserListItemViewModel
    {
        public Guid Id { get; set; }
        public string Position { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public AuthorityType WriteAccess { get; set; }
        public AuthorityType ReadAccess { get; set; }
    }
}
