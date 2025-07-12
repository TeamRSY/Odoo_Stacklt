using System.ComponentModel.DataAnnotations;

namespace Stacklt_Odoo.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int? AcceptedAnswerId { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
        public virtual Answer? AcceptedAnswer { get; set; }
    }
} 