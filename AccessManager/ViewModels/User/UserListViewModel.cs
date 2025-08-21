using AccessManager.Data.Enums;
using AccessManager.ViewModels;
using AccessManager.ViewModels.User;

public class UserListViewModel
{
    public PagedResult<UserListItemViewModel> Users { get; set; } = new();
    public List<string> SortOptions { get; set; } = [];
    public string SelectedSortOption { get; set; } = String.Empty;
    public List<string> FilterUnits { get; set; } = [];
    public string SelectedFilterUnit { get; set; } = String.Empty;
    public List<string> FilterDepartments { get; set; } = [];
    public string SelectedFilterDepartment { get; set; } = String.Empty;
    public AuthorityType WriteAuthority { get; set; }
}
