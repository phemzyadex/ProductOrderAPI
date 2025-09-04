using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogEventAsync(string eventType, string userId, string details);
    }
}
