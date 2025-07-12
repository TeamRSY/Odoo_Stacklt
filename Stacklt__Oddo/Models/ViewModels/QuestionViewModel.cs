using System.ComponentModel.DataAnnotations;

namespace Stacklt_Odoo.Models.ViewModels
{
    public class QuestionViewModel
    {
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title must be between 10 and 200 characters", MinimumLength = 10)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
} 