using AccessManager.ViewModels.InformationSystem;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.Access
{
    public class MapUserAccessViewModel
    {
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public List<SelectListItem> FilterDirectives { get; set; } = [];
        public string FilterDirective1 { get; set; } = String.Empty;
        public string FilterDirective2 { get; set; } = String.Empty;
        public List<AccessViewModel> AccessibleSystems { get; set; } = [];
        public List<AccessViewModel> InaccessibleSystems { get; set; } = [];
        public Guid? DirectiveToRevokeAccess { get; set; }
        public List<Guid> SelectedAccessibleSystemIds { get; set; } = [];
        public Guid? DirectiveToGrantAccess { get; set; }
        public List<Guid> SelectedInaccessibleSystemIds { get; set; } = [];
        public int CurrentPage1 { get; set; } = 1;
        public int TotalPages1 { get; set; } = 1;
        public int CurrentPage2 { get; set; } = 1;
        public int TotalPages2 { get; set; } = 1;
    }
}
