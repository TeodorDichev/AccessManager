namespace AccessManager.ViewModels.User
{
    public class UserListItemViewModel
    {
        public string UserName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Unit { get; set; } = "";
        public string WriteAccess { get; set; } = "";
        public string ReadAccess { get; set; } = "";
    }
}
