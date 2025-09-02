using AccessManager.ViewModels.Department;

namespace AccessManager.ViewModels.Unit
{
    public class MapUserUnitAccessViewModel : IAuthAwareViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public Guid? FilterDepartmentId1 { get; set; }
        public string? FilterDepartmentDescription1 { get; set; }
        public Guid? FilterDepartmentId2 { get; set; }
        public string? FilterDepartmentDescription2 { get; set; }
        public PagedResult<UnitDepartmentViewModel> AccessibleUnits { get; set; } = new();
        public PagedResult<UnitDepartmentViewModel> InaccessibleUnits { get; set; } = new();
        public List<Guid> SelectedAccessibleUnitIds { get; set; } = [];
        public List<Guid> SelectedInaccessibleUnitIds { get; set; } = [];
    }
}
