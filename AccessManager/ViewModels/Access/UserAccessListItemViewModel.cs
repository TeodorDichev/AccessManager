using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.Access
{
    public class UserAccessListItemViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public string AccessDescription { get; set; } = "";
        public string GrantDirectiveDescription { get; set; } = "";
        public string RevokeDirectiveDescription { get; set; } = "";
        public AuthorityType WriteAccess { get; set; }
        public AuthorityType ReadAccess { get; set; }
    }
}
