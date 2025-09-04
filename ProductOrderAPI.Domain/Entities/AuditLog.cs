using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;   // e.g. "OrderPlaced", "StockUpdated"
        public string UserId { get; set; } = string.Empty;      // from JWT or system
        public string Details { get; set; } = string.Empty;     // JSON or plain text
    }
}
