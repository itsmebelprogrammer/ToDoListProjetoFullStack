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

    /// <summary>Registra um novo usuário.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(UserRegisterModel model)
    {
        var user = await _authService.Register(model);
        if (user == null)
            return Problem(title: "E-mail já cadastrado.", statusCode: StatusCodes.Status409Conflict);

        return Ok(new { message = "Usuário registrado com sucesso!" });
    }

    /// <summary>Autentica um usuário e retorna tokens de acesso.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(UserLoginModel model)
    {
        var response = await _authService.Login(model);
        if (response == null)
            return Problem(title: "Credenciais inválidas.", statusCode: StatusCodes.Status401Unauthorized);

        return Ok(response);
    }

    /// <summary>Renova o token de acesso usando um refresh token válido.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenModel model)
    {
        var response = await _authService.Refresh(model.RefreshToken);
        if (response == null)
            return Problem(title: "Refresh token inválido ou expirado.", statusCode: StatusCodes.Status401Unauthorized);

        return Ok(response);
    }
}
