namespace ToDoListProjeto.Api.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<TaskItem> Tasks { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
