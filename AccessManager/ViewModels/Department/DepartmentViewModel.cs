using AccessManager.ViewModels.Unit;

namespace AccessManager.ViewModels.Department
{
    public class DepartmentViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitViewModel> Units { get; set; } = [];
    }
}
