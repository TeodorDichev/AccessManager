using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.Department
{
    public class UnitDepartmentListViewModel
    {
        public PagedResult<UnitDepartmentViewModel> UnitDepartments { get; set; } = new();
        public Guid? FilterDepartmentId { get; set; }
        public string FilterDepartmentDescription { get; set; } = string.Empty;
        public Guid? FilterUnitId { get; set; }
        public string FilterUnitDescription { get; set; } = string.Empty;
        public AuthorityType WriteAuthority { get; set; }
    }
}
