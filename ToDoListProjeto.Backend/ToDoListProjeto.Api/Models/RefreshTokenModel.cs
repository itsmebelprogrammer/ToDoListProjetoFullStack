using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

public class RefreshTokenModel
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
