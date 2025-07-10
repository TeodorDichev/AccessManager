namespace AccessManager.Data.Entities
{
    public class InformationSystem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        virtual public ICollection<Access> Accesses { get; set; } = [];
    }
}
