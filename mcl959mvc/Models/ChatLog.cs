using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Models
{
    [Table("ChatLog")]
    public class ChatLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? UserEmail { get; set; }

        [Required]
        [StringLength(4000)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Answer { get; set; } = string.Empty;

        public bool IsRegistrationHelp { get; set; }
    }
}