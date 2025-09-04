using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Application.Services;
using ProductOrderAPI.Infrastructure.Persistence;
using ProductOrderAPI.Infrastructure.Security;
using System.Data;

namespace ProductOrderAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db, AuthService auth)
        {
            _auth = auth;
            _db = db;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var success = await _auth.RegisterAsync(request.Username, request.Password, request.Role);
            if (!success) return BadRequest("User already exists.");
            return Ok("Registration successful.");
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request, [FromServices] JwtTokenGenerator jwt)
        {
            var user = _db.Users.SingleOrDefault(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = jwt.GenerateToken(user);
            //return Ok(new { token });
            return Ok(new
            {
                token,
                username = user.Username,
                role = user.Role,
                dateTime = DateTime.UtcNow
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _auth.GetAllUsersAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role
            });

            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(userDtos));
        }
    }
}
