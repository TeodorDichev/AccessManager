using AccessManager.ViewModels.User;

public class UserListViewModel
{
    public List<UserListItemViewModel> Users { get; set; } = [];
    public List<string> SortOptions { get; set; } = [];
    public string SelectedSortOption { get; set; } = String.Empty;
    public List<string> FilterUnits { get; set; } = [];
    public string SelectedFilterUnit { get; set; } = String.Empty;
    public List<string> FilterDepartments { get; set; } = [];
    public string SelectedFilterDepartment { get; set; } = String.Empty;

    public bool HasWriteAuthority { get; set; }
    public bool IsSuperAdmin { get; set; }

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
