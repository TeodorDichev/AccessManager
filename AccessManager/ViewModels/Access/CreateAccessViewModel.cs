using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels.Access
{
    public class CreateAccessViewModel
    {
        [Required(ErrorMessage = "Моля въведете име на достъпа")]
        public string Description { get; set; } = string.Empty;

        [Range(0, 5, ErrorMessage = "Ниво трябва да е между 0 и 5")]
        public int Level { get; set; } = 0;

        public Guid? ParentAccessId { get; set; }

        public string? ParentDescription { get; set; }
    }
}
