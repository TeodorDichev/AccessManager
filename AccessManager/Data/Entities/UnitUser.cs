namespace AccessManager.Data.Entities
{
    public class UnitUser
    {
        public Guid UnitId { get; set; }
        virtual public Unit Unit { get; set; } = null!;

        public Guid UserId { get; set; }
        virtual public User User { get; set; } = null!;
        public DateTime? DeletedOn { get; set; } = null;
    }
}
