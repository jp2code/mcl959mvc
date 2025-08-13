using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Models;

public class MemorialViewModel
{
    // Expose the MemorialModel's Id for convenience
    [Key]
    [Column("ID")]
    public int Id => Memorial?.Id ?? 0;
    public MemorialModel Memorial { get; set; }
    public List<CommentsModel> Comments { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime DiedOn { get; set; }
}