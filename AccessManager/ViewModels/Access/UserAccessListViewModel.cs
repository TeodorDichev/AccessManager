namespace AccessManager.ViewModels.Access
{
    public class UserAccessListViewModel : IAuthAwareViewModel
    {
        public PagedResult<UserAccessListItemViewModel> UserAccessList { get; set; } = new();
        public Guid? FilterAccessId { get; set; }

        public string? FilterAccessDescription { get; set; }
        public Guid? FilterUserId { get; set; }

        public string? FilterUserName { get; set; }
        public Guid? FilterDirectiveId { get; set; }

        public string? FilterDirectiveDescription { get; set; }
    }
}
