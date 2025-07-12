using System.ComponentModel.DataAnnotations;

namespace Stacklt_Odoo.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        
        [StringLength(255)]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
} 