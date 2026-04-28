using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

public class TaskCreateModel
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "O status é obrigatório.")]
    [ValidValue("Pendente", "Em Andamento", "Concluída")]
    public string Status { get; set; } = "Pendente";

    [Required(ErrorMessage = "A prioridade é obrigatória.")]
    [ValidValue("Baixa", "Média", "Alta")]
    public string Priority { get; set; } = "Média";
}
