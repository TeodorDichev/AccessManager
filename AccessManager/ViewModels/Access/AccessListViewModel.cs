using AccessManager.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.Access
{
    public class AccessListViewModel
    {
        public PagedResult<AccessListItemViewModel> Accesses { get; set; } = new();

        public Guid? FilterAccessId { get; set; }

        public string FilterAccessDescription { get; set; } = string.Empty;

        public AuthorityType WriteAuthority { get; set; }
    }
}
