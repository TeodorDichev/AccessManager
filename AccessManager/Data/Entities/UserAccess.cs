namespace AccessManager.Data.Entities
{
    public class UserAccess
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        virtual public User User { get; set; } = null!;

        public Guid AccessId { get; set; }
        virtual public Access Access { get; set; } = null!;

        public Guid GrantedByDirectiveId { get; set; }
        virtual public Directive GrantedByDirective { get; set; } = null!;
        public DateTime GrantedOn { get; set; }

        public Guid? RevokedByDirectiveId { get; set; }
        virtual public Directive? RevokedByDirective { get; set; } = null!;

        public DateTime? RevokedOn { get; set; }
        public DateTime? DeletedOn { get; set; } = null;
    }
}
