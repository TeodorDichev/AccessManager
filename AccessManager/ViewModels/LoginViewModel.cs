using AccessManager.Utills;
using System.ComponentModel.DataAnnotations;

namespace AccessManager.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        required public string Username { get; set; }

        [Required(ErrorMessage = ExceptionMessages.RequiredField)]
        [DataType(DataType.Password)]
        required public string Password { get; set; }
    }

}
