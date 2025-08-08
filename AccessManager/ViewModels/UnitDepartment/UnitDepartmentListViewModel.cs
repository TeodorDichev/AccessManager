using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.UnitDepartment
{
    public class UnitDepartmentListViewModel
    {
        public List<DepartmentViewModel> Departments { get; set; } = [];
        public AuthorityType WriteAuthority { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

    }
}
