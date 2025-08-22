using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.Unit
{
    public class CreateUnitViewModel
    {
        public string UnitName { get; set; } = string.Empty;
        public Guid? SelectedDepartmentId { get; set; }
    }
}
