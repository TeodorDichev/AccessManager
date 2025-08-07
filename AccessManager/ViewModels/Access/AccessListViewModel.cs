using AccessManager.Data.Enums;
using AccessManager.ViewModels.InformationSystem;

namespace AccessManager.ViewModels.Access
{
    public class AccessListViewModel
    {
        public List<AccessListItemViewModel> Accesses { get; set; } = [];
        public AuthorityType WriteAuthority { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
