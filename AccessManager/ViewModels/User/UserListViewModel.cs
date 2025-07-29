namespace AccessManager.ViewModels.User
{
    public class UserListViewModel
    {
        public List<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
        public List<string> SortOptions { get; set; } = new List<string> { "WriteAccess", "ReadAccess", "UserName" };
        public string SelectedSortOption { get; set; } = "WriteAccess";
        public List<string> FilterUnits { get; set; } = new List<string>();
        public List<string> FilterDepartments { get; set; } = new List<string>();
        public string SelectedFilterUnit { get; set; } = "";
        public string SelectedFilterDepartment { get; set; } = "";
        public bool CanAddUser { get; set; } = false;
    }
}
