using AccessManager.Data.Enums;
using AccessManager.Utills;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.Unit;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class MyProfileViewModel
    {
        public AuthorityType ReadingAccess { get; set; }
        public AuthorityType WritingAccess { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Моля изберете дирекция")]
        public Guid? SelectedDepartmentId { get; set; }

        [Required(ErrorMessage = "Моля изберете отдел")]
        public Guid SelectedUnitId { get; set; }

        public List<SelectListItem> AvailableDepartments { get; set; } = [];
        public List<SelectListItem> AvailableUnits { get; set; } = [];

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; }
        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]

        public string? Phone { get; set; } = string.Empty;
        public List<UnitViewModel> AccessibleUnits { get; set; } = [];
        public List<AccessViewModel> UserAccesses { get; set; } = [];
    }
}
