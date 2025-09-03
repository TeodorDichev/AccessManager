using AccessManager.Data.Enums;
using AccessManager.ViewModels.User;

namespace AccessManager.ViewModels.Unit
{
    public class UnitEditViewModel : IAuthAwareViewModel
    {
        public Guid UnitId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public PagedResult<UserListItemViewModel> UsersWithAccess { get; set; } = new();
    }
}
