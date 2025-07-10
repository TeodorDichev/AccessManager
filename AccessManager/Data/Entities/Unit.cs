namespace AccessManager.Data.Entities
{
    public class Unit
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public virtual Department Department { get; set; } = null!;
        virtual public ICollection<User> UsersFromUnit { get; set; } = [];
        virtual public ICollection<UnitUser> UsersWithAccess { get; set; } = [];
        public DateTime? DeletedOn { get; set; } = null;
    }
}
