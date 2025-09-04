using Microsoft.Extensions.Logging;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;

namespace ProductOrderAPI.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AppDbContext db, ILogger<AuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogEventAsync(string eventType, string userId, string details)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EventType = eventType,
                    UserId = userId,
                    Details = details
                };

                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Audit event saved: {EventType}, User={UserId}", eventType, userId);
            }
            catch (Exception ex)
            {
                // Fall back to normal logging if DB fails
                _logger.LogError(ex, "Failed to save audit log for event {EventType}", eventType);
            }
        }
    }
}
