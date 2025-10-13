using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MovieTickets.Core.Entities;

namespace MovieTickets.Api.Services;

public sealed class JwtTokenService
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "movie-tickets";
        public string Audience { get; set; } = "movie-tickets-clients";
        public string Secret { get; set; } = "dev-only-change-me-please";
        public int ExpMinutes { get; set; } = 120;
        public AdminSeed? Admin { get; set; }
    }

    public sealed class AdminSeed
    {
        public string Email { get; set; } = "admin@example.com";
        public string Password { get; set; } = "Admin@123";
        public string DisplayName { get; set; } = "Admin";
    }

    private readonly JwtOptions _opt;
    private readonly byte[] _key;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _opt = options.Value;
        _key = Encoding.UTF8.GetBytes(_opt.Secret);
    }

    public string Create(User user)
    {
        var handler = new JwtSecurityTokenHandler();
        var desc = new SecurityTokenDescriptor
        {
            Issuer = _opt.Issuer,
            Audience = _opt.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_opt.ExpMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(desc);
        return handler.WriteToken(token);
    }
}
