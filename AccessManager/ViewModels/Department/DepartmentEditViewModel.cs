using AccessManager.ViewModels.Unit;

namespace AccessManager.ViewModels.Department
{
    public class DepartmentEditViewModel : IAuthAwareViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public PagedResult<UnitViewModel> Units { get; set; } = new();
    }
}
