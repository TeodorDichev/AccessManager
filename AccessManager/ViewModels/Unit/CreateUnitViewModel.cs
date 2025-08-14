using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.Unit
{
    public class CreateUnitViewModel
    {
        public string UnitName { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public List<SelectListItem> Departments { get; set; } = [];
    }
}
