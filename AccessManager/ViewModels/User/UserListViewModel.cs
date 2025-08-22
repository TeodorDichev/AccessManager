using AccessManager.Data.Enums;
using AccessManager.ViewModels;
using AccessManager.ViewModels.User;

public class UserListViewModel
{
    public PagedResult<UserListItemViewModel> Users { get; set; } = new();
    public UserSortOptions SelectedSortOption { get; set; }
    public Guid? FilterDepartmentId { get; set; }
    public string? FilterDepartmentDescription { get; set; }
    public Guid? FilterUnitId { get; set; }
    public string? FilterUnitDescription { get; set; }
    public AuthorityType WriteAuthority { get; set; }
}
