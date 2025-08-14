using AccessManager.Data.Enums;

namespace AccessManager.ViewModels.UnitDepartment
{
    public class DepartmentEditViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitListItemViewModel> Units { get; set; } = [];
        public AuthorityType WriteAuthority { get; set; }
    }
}
