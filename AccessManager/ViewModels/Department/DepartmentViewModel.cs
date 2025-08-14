namespace AccessManager.ViewModels.UnitDepartment
{
    public class DepartmentViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitViewModel> Units { get; set; } = [];

    }
}
