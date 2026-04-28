using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

public class UserRegisterModel
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 50 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial (@$!%*?&).")]
    public string Password { get; set; } = string.Empty;
}
