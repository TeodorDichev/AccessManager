using AccessManager.ViewModels.InformationSystem;

namespace AccessManager.ViewModels.User
{
    public class MapUserToSystemsViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public List<InformationSystemViewModel> Systems { get; set; } = [];
    }
}
