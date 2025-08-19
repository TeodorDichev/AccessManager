namespace AccessManager.ViewModels.Department
{
    public class UnitDepartmentViewModel
    {
        public Guid? UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public Guid? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }
}
