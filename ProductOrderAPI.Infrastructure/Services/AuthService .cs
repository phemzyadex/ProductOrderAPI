using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using ProductOrderAPI.Infrastructure.Repositories;
using ProductOrderAPI.Infrastructure.Security;

namespace ProductOrderAPI.Application.Services
{
    public class AuthService
    {
        private readonly UserRepository _users;
        private readonly JwtTokenGenerator _jwt;

        public AuthService(UserRepository users, JwtTokenGenerator jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        public async Task<bool> RegisterAsync(string username, string password, string role)
        {
            var existing = await _users.GetByUsernameAsync(username);
            if (existing != null) return false; // User already exists

            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                Role = role
            };

            await _users.AddUserAsync(user);
            return true;
        }

        public async Task<string?> LoginAsync(string username, string password)
        {
            var user = await _users.GetByUsernameAsync(username);
            if (user == null) return null;

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                return null;

            return _jwt.GenerateToken(user);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _users.GetAllAsync();
        }
    }
}
