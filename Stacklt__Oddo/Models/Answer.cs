using System.ComponentModel.DataAnnotations;

namespace Stacklt_Odoo.Models
{
    public class Answer
    {
        public int AnswerId { get; set; }
        
        public int QuestionId { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Question Question { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public virtual ICollection<Question> QuestionsWhereAccepted { get; set; } = new List<Question>();
    }
} 