using AccessManager.Data.Enums;
using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class EditUserViewModel
    {
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string UserName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string FirstName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string MiddleName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string LastName { get; set; } = null!;
        public string? EGN { get; set; }
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }

        public AuthorityType ReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType WritingAccess { get; set; } = AuthorityType.None;
        public AuthorityType LoggedUserReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType LoggedUserWritingAccess { get; set; } = AuthorityType.None;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid SelectedDepartmentId { get; set; }

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid SelectedUnitId { get; set; }

        public string SelectedDepartmentDescription { get; set; } = null!;
        public string SelectedUnitDescription { get; set; } = null!;
    }
}
