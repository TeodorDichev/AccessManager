using AccessManager.Data.Enums;
using AccessManager.ViewModels.InformationSystem;
using AccessManager.ViewModels.UnitDepartment;

namespace AccessManager.ViewModels.User
{
    public class EditUserViewModel
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EGN { get; set; }
        public string Phone { get; set; }
        public string? NewPassword { get; set; }

        public AuthorityType ReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType WritingAccess { get; set; } = AuthorityType.None;

        public string DepartmentDescription { get; set; }
        public string UnitDescription { get; set; }

        public List<UnitViewModel> AccessibleUnits { get; set; } = new();
        public List<AccessViewModel> UserAccesses { get; set; } = new();
    }
}
