using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KnowledgeBase.Application.Services;

public class AuthService
{
    private readonly IRepository<User> _userRepo;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<User> userRepo, IConfiguration configuration)
    {
        _userRepo = userRepo;
        _configuration = configuration;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string databaseType = "SQLite")
    {
        var user = (await _userRepo.FindAsync(u => u.Username == request.Username)).FirstOrDefault();
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.Fail("用户名或密码错误");

        if (!user.IsActive)
            return ApiResponse<LoginResponse>.Fail("账号已被禁用");

        var token = GenerateJwtToken(user, databaseType);

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(token, user.DisplayName, user.Role.ToString(), user.Id, databaseType));
    }

    private string GenerateJwtToken(User user, string databaseType)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "KnowledgeBaseSecretKey2024!@#$%^&*()_+VeryLongKey"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("DisplayName", user.DisplayName),
            new Claim("DatabaseType", databaseType)
        };
        if (user.DepartmentId.HasValue)
            claims.Add(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "KnowledgeBase",
            audience: _configuration["Jwt:Audience"] ?? "KnowledgeBase",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
