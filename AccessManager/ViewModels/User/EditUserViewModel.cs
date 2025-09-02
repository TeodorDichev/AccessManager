using AccessManager.Data.Enums;
using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class EditUserViewModel : IAuthAwareViewModel
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string UserName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string FirstName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string MiddleName { get; set; } = null!;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string LastName { get; set; } = null!;
        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; }
        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }

        public AuthorityType ReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType WritingAccess { get; set; } = AuthorityType.None;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedDepartmentId { get; set; }
        public string SelectedDepartmentDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedUnitId { get; set; }
        public string SelectedUnitDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedPositionId { get; set; }
        public string SelectedPositionDescription { get; set; } = string.Empty;
    }
}
