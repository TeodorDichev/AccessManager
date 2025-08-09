using AccessManager.Data.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class EditUserViewModel
    {
        [Required]
        public string UserName { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string MiddleName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        public string? EGN { get; set; }
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }

        public AuthorityType ReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType WritingAccess { get; set; } = AuthorityType.None;
        public AuthorityType LoggedUserReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType LoggedUserWritingAccess { get; set; } = AuthorityType.None;

        [Required]
        public Guid SelectedDepartmentId { get; set; }

        [Required]
        public Guid SelectedUnitId { get; set; }

        public List<SelectListItem> AvailableDepartments { get; set; } = [];
        public List<SelectListItem> AvailableUnits { get; set; } = [];
    }
}
