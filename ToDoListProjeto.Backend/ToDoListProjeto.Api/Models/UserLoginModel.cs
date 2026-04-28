using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

public class UserLoginModel
{
    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;
}
