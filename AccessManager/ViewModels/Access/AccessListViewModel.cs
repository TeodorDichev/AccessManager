using AccessManager.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.Access
{
    public class AccessListViewModel
    {
        public PagedResult<AccessListItemViewModel> Accesses { get; set; } = new();

        [Range(0, 5, ErrorMessage = "Ниво трябва да е между 0 и 5")]
        public int Level { get; set; } = 0;
        public Guid? FilterAccessId { get; set; }

        public string? FilterDescription { get; set; }

        public AuthorityType WriteAuthority { get; set; }
    }
}
