namespace AccessManager.ViewModels.User
{
    public class DeletedUserListViewModel
    {
        public PagedResult<UserListItemViewModel> Users { get; set; } = new();
    }
}
