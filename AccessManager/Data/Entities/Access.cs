namespace AccessManager.Data.Entities
{
    public class Access
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid? ParentAccessId { get; set; }
        public virtual Access? ParentAccess { get; set; }
        public virtual ICollection<Access> SubAccesses { get; set; } = [];
        public virtual ICollection<UserAccess> UserAccesses { get; set; } = [];
        public DateTime? DeletedOn { get; set; } = null;
    }
}
