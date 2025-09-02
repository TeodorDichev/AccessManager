using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.User
{
    public class UserAccessViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public AuthorityType WriteAccess { get; set; }
        public AuthorityType ReadAccess { get; set; }
        public Guid DirectiveId { get; set; }
        public string DirectiveDescription { get; set; } = string.Empty;
    }
}
