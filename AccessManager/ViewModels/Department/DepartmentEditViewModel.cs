using AccessManager.Data.Enums;
using AccessManager.ViewModels.Unit;

namespace AccessManager.ViewModels.Department
{
    public class DepartmentEditViewModel
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<UnitViewModel> Units { get; set; } = [];
        public AuthorityType WriteAuthority { get; set; }
    }
}
