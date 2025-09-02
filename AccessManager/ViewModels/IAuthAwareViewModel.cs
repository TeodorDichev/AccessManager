using AccessManager.Data.Enums;

namespace AccessManager.ViewModels
{
    public abstract class IAuthAwareViewModel
    {
        AuthorityType LoggedUserWriteAuthority { get; set; }
        AuthorityType LoggedUserReadAuthority { get; set; }
    }
}
