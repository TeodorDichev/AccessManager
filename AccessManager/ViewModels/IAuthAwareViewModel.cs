using AccessManager.Data.Enums;

namespace AccessManager.ViewModels
{
    public abstract class IAuthAwareViewModel
    {
        public AuthorityType LoggedUserWriteAuthority { get; set; }
        public AuthorityType LoggedUserReadAuthority { get; set; }
    }
}
