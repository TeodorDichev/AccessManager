namespace AccessManager.ViewModels.Department
{
    public class UnitDepartmentListViewModel : IAuthAwareViewModel
    {
        public PagedResult<UnitDepartmentViewModel> UnitDepartments { get; set; } = new();
        public Guid? FilterDepartmentId { get; set; }
        public string FilterDepartmentDescription { get; set; } = string.Empty;
        public Guid? FilterUnitId { get; set; }
        public string FilterUnitDescription { get; set; } = string.Empty;
    }
}
