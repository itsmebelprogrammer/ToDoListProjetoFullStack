using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ToDoListProjeto.Api.Data;
using ToDoListProjeto.Api.Models;

namespace ToDoListProjeto.Api.Services;

public class AuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiryHours;

    public AuthService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret não configurado.");
        _jwtExpiryHours = int.Parse(configuration["Jwt:ExpiryHours"] ?? "8");
    }

    public async Task<AuthResponseModel?> Login(UserLoginModel model)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            return null;

        var refreshToken = CreateRefreshToken(user.Id);
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthResponseModel
        {
            Token = GenerateJwtToken(user),
            RefreshToken = refreshToken.Token
        };
    }

    public async Task<User?> Register(UserRegisterModel model)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == model.Email))
            return null;

        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = model.Name,
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();
        return newUser;
    }

    public async Task<AuthResponseModel?> Refresh(string refreshToken)
    {
        var token = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t =>
                t.Token == refreshToken &&
                !t.IsRevoked &&
                t.ExpiresAt > DateTime.UtcNow);

        if (token == null) return null;

        token.IsRevoked = true;
        var newRefreshToken = CreateRefreshToken(token.UserId);
        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthResponseModel
        {
            Token = GenerateJwtToken(token.User),
            RefreshToken = newRefreshToken.Token
        };
    }

    private RefreshToken CreateRefreshToken(string userId) => new()
    {
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)]),
            Expires = DateTime.UtcNow.AddHours(_jwtExpiryHours),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
