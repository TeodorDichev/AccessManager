using AccessManager.Data.Enums;
using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public ReadingAccess ReadingAccess { get; set; } = ReadingAccess.None;
        public WritingAccess WritingAccess { get; set; } = WritingAccess.None;
        public string UserName { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty; // Nullable to allow adding users with no reading and writing access
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public Guid UnitId { get; set; }
        public virtual Unit Unit { get; set; } = null!; // Means that the user also has a department

        [RegularExpression(@"^\d{10}$", ErrorMessage = ExceptionMessages.InvalidEGN)]
        public string? EGN { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]
        public string? Phone { get; set; } = string.Empty;
        public DateTime? DeletedOn { get; set; } = null;
        virtual public ICollection<UnitUser> AccessibleUnits{ get; set; } = [];
        virtual public ICollection<UserAccess> UserAccesses { get; set; } = [];
    }
}
