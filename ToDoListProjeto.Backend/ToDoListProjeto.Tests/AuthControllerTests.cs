using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ToDoListProjeto.Api.Data;
using ToDoListProjeto.Api.Models;
using ToDoListProjeto.Api.Services;
using Xunit;

namespace ToDoListProjeto.Tests;

public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Secret", "test-secret-key-longa-o-suficiente-para-hmac-sha256" },
                { "Jwt:ExpiryHours", "8" }
            }!)
            .Build();

        _authService = new AuthService(configuration, _dbContext);
        _controller = new AuthController(_authService);
    }

    public void Dispose() => _dbContext.Dispose();

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithNewEmail_Returns200Ok()
    {
        var model = new UserRegisterModel { Name = "Test", Email = "new@mail.com", Password = "Valid@123" };

        var result = await _controller.Register(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsSuccessMessage()
    {
        var model = new UserRegisterModel { Name = "Test", Email = "msg@mail.com", Password = "Valid@123" };

        var result = await _controller.Register(model) as OkObjectResult;

        Assert.NotNull(result);
        var body = result!.Value as dynamic;
        Assert.NotNull(body);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        var model = new UserRegisterModel { Name = "Test", Email = "dup@mail.com", Password = "Valid@123" };
        await _controller.Register(model);

        var result = await _controller.Register(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        var register = new UserRegisterModel { Name = "Login", Email = "login@mail.com", Password = "Login@123" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "login@mail.com", Password = "Login@123" };
        var result = await _controller.Login(login);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var register = new UserRegisterModel { Name = "Token", Email = "token@mail.com", Password = "Token@123" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "token@mail.com", Password = "Token@123" };
        var result = await _controller.Login(login) as OkObjectResult;

        Assert.NotNull(result);
        var response = result!.Value as AuthResponseModel;
        Assert.NotNull(response);
        Assert.NotEmpty(response!.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var register = new UserRegisterModel { Name = "User", Email = "auth@mail.com", Password = "Correct@123" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "auth@mail.com", Password = "Wrong@456" };
        var result = await _controller.Login(login);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        var login = new UserLoginModel { Email = "nobody@mail.com", Password = "Any@123" };

        var result = await _controller.Login(login);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
