using AccessManager.ViewModels.UnitDepartment;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.User
{
    public class MapUserUnitAccessViewModel
    {
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public List<SelectListItem> FilterDepartments { get; set; } = [];
        public string SelectedFilterDepartment1 { get; set; } = String.Empty;
        public string SelectedFilterDepartment2 { get; set; } = String.Empty;
        public List<UnitViewModel> AccessibleUnits { get; set; } = [];
        public List<UnitViewModel> InaccessibleUnits { get; set; } = [];
        public string? SelectedAccessibleUnitIds { get; set; }
        public string? SelectedInaccessibleUnitIds { get; set; }
        public int CurrentPage1 { get; set; } = 1;
        public int TotalPages1 { get; set; } = 1;
        public int CurrentPage2 { get; set; } = 1;
        public int TotalPages2 { get; set; } = 1;
    }
}
