using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.Data.Entities
{
    public class Admin
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [RegularExpression(@"^(?:\+359|0)?8[7-9][0-9]{7}$", ErrorMessage = ExceptionMessages.InvalidPhone)]
        public string Phone { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Admin;
    }
}
