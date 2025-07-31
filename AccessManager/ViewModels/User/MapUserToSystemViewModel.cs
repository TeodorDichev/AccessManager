using AccessManager.ViewModels.InformationSystem;

namespace AccessManager.ViewModels.User
{
    public class MapUserToSystemsViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DepartmentDescription { get; set; } = string.Empty;
        public string UnitDescription { get; set; } = string.Empty;
        public List<InformationSystemViewModel> Systems { get; set; } = [];
    }
}
