using AccessManager.Data.Enums;
using AccessManager.ViewModels.UnitDepartment;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessManager.ViewModels.User
{
    public class CreateUserViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? EGN { get; set; }
        public string? Phone { get; set; }

        public Guid? SelectedDepartmentId { get; set; }
        public Guid? SelectedUnitId { get; set; }

        public ReadingAccess SelectedReadingAccess { get; set; } = ReadingAccess.None;
        public WritingAccess SelectedWritingAccess { get; set; } = WritingAccess.None;

        public string? Password { get; set; }

        public List<Guid> SelectedAccessibleUnitIds { get; set; } = [];

        // Dropdown data
        public List<DepartmentUnitViewModel> DepartmentUnits { get; set; } = [];
        public List<SelectListItem> Departments { get; set; } = [];
        public List<SelectListItem> Units { get; set; } = [];

        public List<SelectListItem> AvailableUnits { get; set; } = [];

        // For checking permissions
        public bool CanAddToAllDepartments { get; set; } = false;
    }
}
