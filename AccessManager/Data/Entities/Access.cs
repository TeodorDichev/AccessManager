namespace AccessManager.Data.Entities
{
    public class Access
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid SystemId { get; set; }
        public virtual InformationSystem System { get; set; } = null!;
        public virtual ICollection<UserAccess> UserAccesses { get; set; } = [];
        public virtual ICollection<Access> SubAccesses { get; set; } = [];
        public DateTime? DeletedOn { get; set; } = null;
    }
}
