using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OrderTrack.API.DTOs;
using OrderTrack.API.Models;
using OrderTrack.API.Data;
using System;

namespace OrderTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly OrderTrackDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(OrderTrackDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthDTO authDTO)
        {
            if (_context.Users.Any(u => u.Username == authDTO.Username))
                return BadRequest("Username already exists.");

            var user = new User
            {
                Username = authDTO.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(authDTO.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthDTO authDTO)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == authDTO.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(authDTO.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            });
        }
    }
}

