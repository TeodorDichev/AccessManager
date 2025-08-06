using AccessManager.Data.Entities;
using AccessManager.ViewModels.InformationSystem;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.User
{
    public class MapUserAccessViewModel
    {
        [Required]
        public string UserName { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string MiddleName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        public List<Directive> Directives { get; set; } = [];
        // TO DO
    }
}
