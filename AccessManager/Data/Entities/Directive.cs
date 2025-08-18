namespace AccessManager.Data.Entities
{
    public class Directive
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime? DeletedOn { get; set; }
    }
}
