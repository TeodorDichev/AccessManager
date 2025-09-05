using AccessManager.Data.Enums;
using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class CreateUserViewModel : IAuthAwareViewModel, IValidatableObject
    {
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string MiddleName { get; set; } = null!;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string LastName { get; set; } = null!;

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]
        public string? Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedDepartmentId { get; set; }
        public string SelectedDepartmentDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedUnitId { get; set; }
        public string SelectedUnitDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedPositionId { get; set; }
        public string SelectedPositionDescription { get; set; } = string.Empty;

        public AuthorityType SelectedReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType SelectedWritingAccess { get; set; } = AuthorityType.None;

        public string? Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SelectedReadingAccess > AuthorityType.None || SelectedWritingAccess > AuthorityType.None)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    yield return new ValidationResult(
                        ExceptionMessages.RequiredField,
                        new[] { nameof(Password) });
                }
            }
        }
    }
}
