namespace AccessManager.ViewModels.User
{
    public class DeletedUserListViewModel
    {
        public List<UserListItemViewModel> Users { get; set; } = [];
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
