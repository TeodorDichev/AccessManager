using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.User;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.Access
{
    public class EditAccessViewModel
    {
        public string Description { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid AccessId { get; set; }
        public List<SelectListItem> FilterDirectives { get; set; } = [];
        public string FilterDirective1 { get; set; } = String.Empty;
        public string FilterDirective2 { get; set; } = String.Empty;
        public List<UserAccessViewModel> UsersWithAccess { get; set; } = [];
        public List<UserAccessViewModel> UsersWithoutAccess { get; set; } = [];
        public string? DirectiveToRevokeAccess { get; set; }
        public List<Guid> SelectedUsersWithAccessIds { get; set; } = new();
        public List<Guid> SelectedUsersWithoutAccessIds { get; set; } = new();
        public string? DirectiveToGrantAccess { get; set; }
        public int CurrentPage1 { get; set; } = 1;
        public int TotalPages1 { get; set; } = 1;
        public int CurrentPage2 { get; set; } = 1;
        public int TotalPages2 { get; set; } = 1;

    }
}
