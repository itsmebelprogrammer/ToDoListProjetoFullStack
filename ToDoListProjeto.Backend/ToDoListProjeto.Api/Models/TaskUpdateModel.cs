using System.ComponentModel.DataAnnotations;

namespace ToDoListProjeto.Api.Models;

public class TaskUpdateModel
{
    [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    public string? Title { get; set; }

    [MaxLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    public string? Description { get; set; }

    [ValidValue("Pendente", "Em Andamento", "Concluída")]
    public string? Status { get; set; }

    [ValidValue("Baixa", "Média", "Alta")]
    public string? Priority { get; set; }
}
