using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ToDoListProjeto.Api.Data;
using ToDoListProjeto.Api.Models;
using ToDoListProjeto.Api.Services;
using Xunit;

namespace ToDoListProjeto.Tests;

public class TasksControllerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TasksController _controller;
    private const string TestUserId = "user-test-123";
    private const string OtherUserId = "user-other-456";

    public TasksControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Gemini:ApiKey", "test-key" } }!)
            .Build();

        // Monta AIService com HttpClient mockado para não chamar API real
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"candidates":[{"content":{"parts":[{"text":"{\"title\":\"IA Task\",\"description\":\"desc\",\"status\":\"Pendente\",\"priority\":\"Média\"}"}]}}]}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var aiService = new AIService(new HttpClient(mockHandler.Object), configuration, NullLogger<AIService>.Instance);
        _controller = new TasksController(_dbContext, aiService);

        SetAuthenticatedUser(TestUserId);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SetAuthenticatedUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private async Task SeedAsync()
    {
        _dbContext.TaskItems.AddRange(
            new TaskItem { Title = "Minha Tarefa 1", Status = "Pendente", Priority = "Alta", UserId = TestUserId },
            new TaskItem { Title = "Minha Tarefa 2", Status = "Concluída", Priority = "Baixa", UserId = TestUserId },
            new TaskItem { Title = "Tarefa de Outro Usuário", Status = "Pendente", Priority = "Média", UserId = OtherUserId }
        );
        await _dbContext.SaveChangesAsync();
    }

    // ─── GetTasks ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_ReturnsOnlyCurrentUserTasks()
    {
        await SeedAsync();

        var result = await _controller.GetTasks();

        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(result.Value);
        Assert.Equal(2, tasks.Count());
        Assert.All(tasks, t => Assert.Equal(TestUserId, t.UserId));
    }

    [Fact]
    public async Task GetTasks_DoesNotReturnOtherUserTasks()
    {
        await SeedAsync();

        var result = await _controller.GetTasks();

        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(result.Value);
        Assert.DoesNotContain(tasks, t => t.UserId == OtherUserId);
    }

    // ─── GetTask ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTask_WithValidIdAndOwner_ReturnsTask()
    {
        await SeedAsync();
        var task = await _dbContext.TaskItems.FirstAsync(t => t.UserId == TestUserId);

        var result = await _controller.GetTask(task.Id);

        Assert.Equal(task.Id, result.Value?.Id);
    }

    [Fact]
    public async Task GetTask_WithOtherUserTask_ReturnsNotFound()
    {
        await SeedAsync();
        var otherTask = await _dbContext.TaskItems.FirstAsync(t => t.UserId == OtherUserId);

        var result = await _controller.GetTask(otherTask.Id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetTask_WithNonExistentId_ReturnsNotFound()
    {
        var result = await _controller.GetTask(99999);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ─── PostTask ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostTask_CreatesTaskWithCurrentUserId()
    {
        var model = new TaskCreateModel { Title = "Nova Tarefa", Status = "Pendente", Priority = "Média" };

        var result = await _controller.PostTask(model);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var task = Assert.IsType<TaskItem>(created.Value);
        Assert.Equal(TestUserId, task.UserId);
    }

    [Fact]
    public async Task PostTask_PersistsTaskToDatabase()
    {
        var model = new TaskCreateModel { Title = "Persist Task", Status = "Pendente", Priority = "Alta" };

        await _controller.PostTask(model);

        var taskInDb = await _dbContext.TaskItems.FirstOrDefaultAsync(t => t.Title == "Persist Task");
        Assert.NotNull(taskInDb);
    }

    [Fact]
    public async Task PostTask_Returns201Created()
    {
        var model = new TaskCreateModel { Title = "Task 201", Status = "Pendente", Priority = "Baixa" };

        var result = await _controller.PostTask(model);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task PostTask_SetsCreatedAtToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var model = new TaskCreateModel { Title = "Time Task", Status = "Pendente", Priority = "Média" };

        var result = await _controller.PostTask(model);

        var created = result.Result as CreatedAtActionResult;
        var task = created!.Value as TaskItem;
        Assert.True(task!.CreatedAt >= before);
    }

    // ─── PutTask ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PutTask_UpdatesTitle_ReturnsNoContent()
    {
        await SeedAsync();
        var task = await _dbContext.TaskItems.FirstAsync(t => t.UserId == TestUserId);
        var update = new TaskUpdateModel { Title = "Título Atualizado" };

        var result = await _controller.PutTask(task.Id, update);

        Assert.IsType<NoContentResult>(result);
        var updated = await _dbContext.TaskItems.FindAsync(task.Id);
        Assert.Equal("Título Atualizado", updated!.Title);
    }

    [Fact]
    public async Task PutTask_WhenStatusChangesToConcluida_SetsCompletedAt()
    {
        await SeedAsync();
        var task = await _dbContext.TaskItems.FirstAsync(t => t.UserId == TestUserId && t.Status == "Pendente");

        await _controller.PutTask(task.Id, new TaskUpdateModel { Status = "Concluída" });

        var updated = await _dbContext.TaskItems.FindAsync(task.Id);
        Assert.NotNull(updated!.CompletedAt);
    }

    [Fact]
    public async Task PutTask_WhenStatusChangesFromConcluida_ClearsCompletedAt()
    {
        await SeedAsync();
        var task = await _dbContext.TaskItems.FirstAsync(t => t.UserId == TestUserId && t.Status == "Concluída");

        await _controller.PutTask(task.Id, new TaskUpdateModel { Status = "Pendente" });

        var updated = await _dbContext.TaskItems.FindAsync(task.Id);
        Assert.Null(updated!.CompletedAt);
    }

    [Fact]
    public async Task PutTask_WithOtherUserTask_ReturnsNotFound()
    {
        await SeedAsync();
        var otherTask = await _dbContext.TaskItems.FirstAsync(t => t.UserId == OtherUserId);

        var result = await _controller.PutTask(otherTask.Id, new TaskUpdateModel { Title = "Invadido" });

        Assert.IsType<NotFoundResult>(result);
    }

    // ─── DeleteTask ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_RemovesOwnTask_ReturnsNoContent()
    {
        await SeedAsync();
        var task = await _dbContext.TaskItems.FirstAsync(t => t.UserId == TestUserId);
        var taskId = task.Id;

        var result = await _controller.DeleteTask(taskId);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await _dbContext.TaskItems.FindAsync(taskId));
    }

    [Fact]
    public async Task DeleteTask_WithOtherUserTask_ReturnsNotFound()
    {
        await SeedAsync();
        var otherTask = await _dbContext.TaskItems.FirstAsync(t => t.UserId == OtherUserId);

        var result = await _controller.DeleteTask(otherTask.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteTask_WithNonExistentId_ReturnsNotFound()
    {
        var result = await _controller.DeleteTask(99999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteTask_DoesNotRemoveOtherUserTasks()
    {
        await SeedAsync();
        var otherTask = await _dbContext.TaskItems.FirstAsync(t => t.UserId == OtherUserId);

        await _controller.DeleteTask(otherTask.Id);

        Assert.NotNull(await _dbContext.TaskItems.FindAsync(otherTask.Id));
    }
}
