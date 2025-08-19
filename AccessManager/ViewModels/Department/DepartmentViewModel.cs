namespace AccessManager.ViewModels.Department
{
    public class DepartmentViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitDepartmentViewModel> Units { get; set; } = [];
    }
}
