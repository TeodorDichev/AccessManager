using AccessManager.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.Access
{
    public class AccessListViewModel : IAuthAwareViewModel
    {
        public PagedResult<AccessListItemViewModel> Accesses { get; set; } = new();

        public Guid? FilterAccessId { get; set; }

        public string FilterAccessDescription { get; set; } = string.Empty;
    }
}
