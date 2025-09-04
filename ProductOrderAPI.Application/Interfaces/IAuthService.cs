using ProductOrderAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.Interfaces
{
    public interface IAuthService
    {
        string GenerateJwtToken(User user);

        Task<User?> ValidateUserAsync(string username, string password);
    }
}
