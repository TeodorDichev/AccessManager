namespace AccessManager.Data.Entities
{
    public class UserAccess
    {
        public Guid UserId { get; set; }
        virtual public User User { get; set; } = null!;

        public Guid AccessId { get; set; }
        virtual public Access Access { get; set; } = null!;

        public Guid DirectiveId { get; set; }
        virtual public Directive Directive { get; set; } = null!;

        public DateTime GrantedOn { get; set; }
        public DateTime? DeletedOn { get; set; } = null;
    }
}
