using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.Department
{
    public class UnitDepartmentListViewModel
    {
        public PagedResult<UnitDepartmentViewModel> UnitDepartments { get; set; } = new();
        public Guid? FilterDepartmentId { get; set; }
        public string? FilterDepartmentDescription { get; set; }
        public AuthorityType WriteAuthority { get; set; }
    }
}
