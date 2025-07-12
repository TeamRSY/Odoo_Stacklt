namespace Stacklt_Odoo.Models
{
    public class QuestionTag
    {
        public int QuestionId { get; set; }
        public int TagId { get; set; }
        
        // Navigation properties
        public virtual Question Question { get; set; } = null!;
        public virtual Tag Tag { get; set; } = null!;
    }
} 