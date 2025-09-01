using AccessManager.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public AuthorityType ReadingAccess { get; set; } = AuthorityType.None;
        public AuthorityType WritingAccess { get; set; } = AuthorityType.None;
        public string UserName { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid? PositionId { get; set; }
        public virtual Position? Position { get; set; }
        public Guid UnitId { get; set; }
        public virtual Unit Unit { get; set; } = null!;

        [RegularExpression(@"^\d{10}$")]
        public string? EGN { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$")]
        public string? Phone { get; set; } = string.Empty;
        public DateTime? DeletedOn { get; set; } = null;
        virtual public ICollection<UnitUser> AccessibleUnits { get; set; } = [];
        virtual public ICollection<UserAccess> UserAccesses { get; set; } = [];
    }
}
