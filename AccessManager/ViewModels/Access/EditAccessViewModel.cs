using AccessManager.ViewModels.User;

namespace AccessManager.ViewModels.Access
{
    public class EditAccessViewModel : IAuthAwareViewModel
    {
        public string Description { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid AccessId { get; set; }

        public Guid? FilterDirectiveId1 { get; set; }
        public string FilterDirectiveDescription1 { get; set; } = string.Empty;

        public Guid? FilterDirectiveId2 { get; set; }
        public string FilterDirectiveDescription2 { get; set; } = string.Empty;

        public PagedResult<UserAccessViewModel> UsersWithAccess { get; set; } = new();
        public PagedResult<UserAccessViewModel> UsersWithoutAccess { get; set; } = new();

        public Guid? DirectiveToRevokeAccessId { get; set; }
        public string DirectiveToRevokeAccessDescription { get; set; } = string.Empty;
        public List<Guid> SelectedUsersWithAccessIds { get; set; } = new();
        public List<Guid> SelectedUsersWithoutAccessIds { get; set; } = new();
        public Guid? DirectiveToGrantAccessId { get; set; }
        public string DirectiveToGrantAccessDescription { get; set; } = string.Empty;
    }
}
