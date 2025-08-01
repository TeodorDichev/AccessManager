using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        required public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        required public string Password { get; set; }
    }

}
