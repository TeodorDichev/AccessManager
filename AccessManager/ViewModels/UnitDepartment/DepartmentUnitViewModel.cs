namespace AccessManager.ViewModels.UnitDepartment
{
    public class DepartmentUnitViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitViewModel> Units { get; set; } = [];
    }
}
