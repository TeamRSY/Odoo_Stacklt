using System.ComponentModel.DataAnnotations;

namespace Stacklt_Odoo.Models
{
    public class Tag
    {
        public int TagId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TagName { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    }
} 