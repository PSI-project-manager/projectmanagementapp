using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private AppDbContext db;
    private PasswordHasher<User> hasher;
    private IConfiguration config;

    public AuthController(AppDbContext db, PasswordHasher<User> hasher, IConfiguration config)
    {
        this.db = db;
        this.hasher = hasher;
        this.config = config;
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        string email = request.Email.Trim().ToLower();

        // find the user
        User? user = db.Users.FirstOrDefault(u => u.Email.ToLower() == email);
        if (user == null || user.Isactive == false)
        {
            return Unauthorized(new { detail = "Invalid email or password." });
        }

        // check the password
        var result = hasher.VerifyHashedPassword(user, user.Passwordhash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { detail = "Invalid email or password." });
        }

        string token = MakeToken(user);
        return Ok(new LoginResponse(token, user.Userid, user.Email, user.Fullname));
    }

    // makes a jwt token that lasts 8 hours
    private string MakeToken(User user)
    {
        string signingKey = config["Jwt:SigningKey"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>();
        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Userid.ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
