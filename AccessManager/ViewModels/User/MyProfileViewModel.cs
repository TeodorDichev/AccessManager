using AccessManager.Data.Enums;
using AccessManager.Utills;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class MyProfileViewModel
    {
        public Guid Id { get; set; }
        public AuthorityType ReadingAccess { get; set; }
        public AuthorityType WritingAccess { get; set; }

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]

        public string MiddleName { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]

        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid? SelectedDepartmentId { get; set; }

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        public Guid SelectedUnitId { get; set; }

        public string? SelectedDepartmentDescription { get; set; }
        public string? SelectedUnitDescription { get; set; }

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; }
        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]

        public string? Phone { get; set; } = string.Empty;
    }
}
