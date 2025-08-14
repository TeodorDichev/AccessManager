using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.Department
{
    public class UnitDepartmentListViewModel
    {
        public List<DepartmentViewModel> Departments { get; set; } = [];
        public List<string> FilterDepartments { get; set; } = [];
        public string SelectedFilterDepartment { get; set; } = String.Empty;
        public AuthorityType WriteAuthority { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
