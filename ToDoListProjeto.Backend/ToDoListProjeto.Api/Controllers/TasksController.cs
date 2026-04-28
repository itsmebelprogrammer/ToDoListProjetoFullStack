using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ToDoListProjeto.Api.Data;
using ToDoListProjeto.Api.Models;
using ToDoListProjeto.Api.Services;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AIService _aiService;

    public TasksController(ApplicationDbContext dbContext, AIService aiService)
    {
        _dbContext = dbContext;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.TaskItems.Where(t => t.UserId == userId);
        var total = await query.CountAsync();

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)total / pageSize)).ToString());

        return tasks;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTask(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var taskItem = await _dbContext.TaskItems
            .Where(t => t.Id == id && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (taskItem == null) return NotFound();
        return taskItem;
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> PostTask(TaskCreateModel model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var newTaskItem = new TaskItem
        {
            Title = model.Title,
            Description = model.Description,
            Status = model.Status,
            Priority = model.Priority,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskItems.Add(newTaskItem);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTask), new { id = newTaskItem.Id }, newTaskItem);
    }

    [HttpPost("smart-add")]
    public async Task<ActionResult<TaskItem>> PostSmartTask([FromBody] SmartTaskModel model)
    {
        var parsedTask = await _aiService.ParseTaskFromPrompt(model.Prompt);

        if (parsedTask == null || string.IsNullOrWhiteSpace(parsedTask.Title))
            return BadRequest(new { message = "Não foi possível interpretar a tarefa a partir do texto fornecido." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var newTaskItem = new TaskItem
        {
            Title = parsedTask.Title,
            Description = parsedTask.Description,
            Status = parsedTask.Status,
            Priority = parsedTask.Priority,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskItems.Add(newTaskItem);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTask), new { id = newTaskItem.Id }, newTaskItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTask(int id, TaskUpdateModel model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existingTask = await _dbContext.TaskItems
            .Where(t => t.Id == id && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (existingTask == null) return NotFound();

        if (model.Title != null) existingTask.Title = model.Title;
        if (model.Description != null) existingTask.Description = model.Description;
        if (model.Status != null)
        {
            existingTask.Status = model.Status;
            existingTask.CompletedAt = model.Status == "Concluída" ? DateTime.UtcNow : null;
        }
        if (model.Priority != null) existingTask.Priority = model.Priority;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var taskItem = await _dbContext.TaskItems
            .Where(t => t.Id == id && t.UserId == userId)
            .FirstOrDefaultAsync();

        if (taskItem == null) return NotFound();

        _dbContext.TaskItems.Remove(taskItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public class SmartTaskModel
{
    public string Prompt { get; set; } = string.Empty;
}
