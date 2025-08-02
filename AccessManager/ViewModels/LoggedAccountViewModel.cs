using AccessManager.Utills;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.UnitDepartment;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels
{
    public class LoggedAccountViewModel
    {
        public string ReadingAccess { get; set; } = string.Empty;
        public string WritingAccess { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UnitDescription { get; set; } = string.Empty;
        public string DepartmentDescription { get; set; } = string.Empty;

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; }
        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]

        public string? Phone { get; set; } = string.Empty;
        public List<UnitViewModel> AccessibleUnits { get; set; } = [];
        public List<AccessViewModel> UserAccesses { get; set; } = [];
        public bool canEditUserName { get; set; }
        public bool canEdit { get; set; }
    }
}
