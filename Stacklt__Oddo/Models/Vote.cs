namespace Stacklt_Odoo.Models
{
    public class Vote
    {
        public int VoteId { get; set; }
        public int AnswerId { get; set; }
        public int UserId { get; set; }
        public bool VoteType { get; set; } // true for upvote, false for downvote
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Answer Answer { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
} 