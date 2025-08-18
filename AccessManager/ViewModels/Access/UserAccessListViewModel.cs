using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.Access
{
    public class UserAccessListViewModel
    {
        public List<UserAccessListItemViewModel> UserAccessList { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public Guid? FilterAccessId { get; set; }

        public string? FilterAccessDescription { get; set; }
        public Guid? FilterUserId { get; set; }

        public string? FilterUserName { get; set; }
        public Guid? FilterDirectiveId { get; set; }

        public string? FilterDirectiveDescription{ get; set; }
    }
}
