namespace AccessManager.Data.Entities
{
    public class Department
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        virtual public ICollection<Unit> Units { get; set; } = [];
        public DateTime? DeletedOn { get; set; } = null;
    }
}
