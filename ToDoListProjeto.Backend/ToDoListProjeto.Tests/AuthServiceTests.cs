using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ToDoListProjeto.Api.Data;
using ToDoListProjeto.Api.Models;
using ToDoListProjeto.Api.Services;
using Xunit;

namespace ToDoListProjeto.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthService _authService;

    public AuthServiceTests()
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
    }

    public void Dispose() => _dbContext.Dispose();

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithNewEmail_ReturnsUser()
    {
        var model = new UserRegisterModel { Name = "Test User", Email = "test@example.com", Password = "Teste@123" };

        var result = await _authService.Register(model);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsNull()
    {
        var first = new UserRegisterModel { Name = "User One", Email = "dup@example.com", Password = "Teste@123" };
        await _authService.Register(first);

        var duplicate = new UserRegisterModel { Name = "User Two", Email = "dup@example.com", Password = "Other@456" };
        var result = await _authService.Register(duplicate);

        Assert.Null(result);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlaintext()
    {
        var plainPassword = "Teste@123";
        var model = new UserRegisterModel { Name = "Hash User", Email = "hash@example.com", Password = plainPassword };

        var result = await _authService.Register(model);

        Assert.NotNull(result);
        Assert.NotEqual(plainPassword, result.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, result.PasswordHash));
    }

    [Fact]
    public async Task Register_PersistsUserToDatabase()
    {
        var model = new UserRegisterModel { Name = "Persist User", Email = "persist@example.com", Password = "Teste@123" };

        await _authService.Register(model);

        var userInDb = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == "persist@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("Persist User", userInDb.Name);
    }

    [Fact]
    public async Task Register_GeneratesUniqueId()
    {
        var m1 = new UserRegisterModel { Name = "A", Email = "a@example.com", Password = "Teste@123" };
        var m2 = new UserRegisterModel { Name = "B", Email = "b@example.com", Password = "Teste@123" };

        var u1 = await _authService.Register(m1);
        var u2 = await _authService.Register(m2);

        Assert.NotNull(u1);
        Assert.NotNull(u2);
        Assert.NotEqual(u1!.Id, u2!.Id);
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        var register = new UserRegisterModel { Name = "Login User", Email = "login@example.com", Password = "Valid@123" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "login@example.com", Password = "Valid@123" };
        var result = await _authService.Login(login);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsNull()
    {
        var register = new UserRegisterModel { Name = "User", Email = "wrong@example.com", Password = "Correct@123" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "wrong@example.com", Password = "Wrong@456" };
        var result = await _authService.Login(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsNull()
    {
        var login = new UserLoginModel { Email = "notfound@example.com", Password = "Any@123" };

        var result = await _authService.Login(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_GeneratesNonEmptyJwtToken()
    {
        var register = new UserRegisterModel { Name = "Jwt User", Email = "jwt@example.com", Password = "Jwt@123!" };
        await _authService.Register(register);

        var login = new UserLoginModel { Email = "jwt@example.com", Password = "Jwt@123!" };
        var result = await _authService.Login(login);

        Assert.NotNull(result);
        Assert.True(result.Token.Length > 20);
    }
}
