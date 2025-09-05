using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.Access
{
    public class UserAccessListViewModel : IAuthAwareViewModel
    {
        public PagedResult<UserAccessListItemViewModel> UserAccessList { get; set; } = new();
        public UserSortOptions SelectedSortOption { get; set; }

        public Guid? FilterAccessId { get; set; }

        public string? FilterAccessDescription { get; set; }
        public Guid? FilterUserId { get; set; }

        public string? FilterUserName { get; set; }
        public Guid? FilterDirectiveId { get; set; }

        public string? FilterDirectiveDescription { get; set; }
        public Guid? FilterPositionId { get; set; }
        public string FilterPositionDescription { get; set; } = string.Empty;
        public Guid? FilterUnitId { get; set; }
        public string FilterUnitDescription { get; set; } = string.Empty;
        public Guid? FilterDepartmentId { get; set; }
        public string FilterDepartmentDescription { get; set; } = string.Empty;
    }
}
