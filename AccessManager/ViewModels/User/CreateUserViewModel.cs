using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels.UnitDepartment;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class CreateUserViewModel : IValidatableObject
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]
        public string? Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Моля изберете дирекция")]
        public Guid? SelectedDepartmentId { get; set; }

        [Required(ErrorMessage = "Моля изберете отдел")]
        public Guid? SelectedUnitId { get; set; }

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
                        "Паролата е задължителна при избран достъп за четене или писане.",
                        new[] { nameof(Password) });
                }
            }
        }

        public List<SelectListItem> Departments { get; set; } = [];
        public List<SelectListItem> Units { get; set; } = [];
        public string? SelectedAccessibleUnitIds { get; set; }
    }
}
