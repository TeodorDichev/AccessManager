namespace AccessManager.Data.Entities
{
    public class Access
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid SystemId { get; set; }
        public virtual InformationSystem System { get; set; } = null!;
        public virtual ICollection<EmployeeAccess> EmployeeAccesses { get; set; } = [];

    }
}
