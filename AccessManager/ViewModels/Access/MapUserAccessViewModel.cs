using AccessManager.ViewModels.InformationSystem;

namespace AccessManager.ViewModels.Access
{
    public class MapUserAccessViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";

        public Guid? FilterDirectiveId1 { get; set; }
        public string? FilterDirectiveDescription1 { get; set; }

        public Guid? FilterDirectiveId2 { get; set; }
        public string? FilterDirectiveDescription2 { get; set; }

        public PagedResult<AccessViewModel> AccessibleSystems { get; set; } = new();
        public PagedResult<AccessViewModel> InaccessibleSystems { get; set; } = new();

        public Guid? DirectiveToRevokeAccessId { get; set; }
        public string DirectiveToRevokeAccessDescription { get; set; } = string.Empty;
        public List<Guid> SelectedAccessibleSystemIds { get; set; } = [];
        public Guid? DirectiveToGrantAccessId { get; set; }
        public List<Guid> SelectedInaccessibleSystemIds { get; set; } = [];
        public string DirectiveToGrantAccessDescription { get; set; } = string.Empty;
    }
}
