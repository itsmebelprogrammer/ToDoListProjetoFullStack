using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToDoListProjeto.Api.Models;
using ToDoListProjeto.Api.Services;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterModel model)
    {
        var user = await _authService.Register(model);
        if (user == null)
            return BadRequest(new { message = "Email já cadastrado." });

        return Ok(new { message = "Usuário registrado com sucesso!" });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(UserLoginModel model)
    {
        var response = await _authService.Login(model);
        if (response == null)
            return Unauthorized(new { message = "Credenciais inválidas." });

        return Ok(response);
    }
}
