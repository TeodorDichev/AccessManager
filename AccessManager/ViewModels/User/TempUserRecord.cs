namespace AccessManager.ViewModels.User
{
    public class TempUserRecord
    {
        public required string UserName { get; set; }
        public required string FirstName { get; set; }
        public required string MiddleName { get; set; }
        public required string LastName { get; set; }
        public required string Position { get; set; }
        public required string Unit { get; set; }
        public required string Department { get; set; }
        public Dictionary<string, string> Accesses { get; set; } = new();
    }
}
