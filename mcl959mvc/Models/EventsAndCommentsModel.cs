using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Models;

public class EventsAndCommentsModel
{
    // Expose the EventModel's Id for convenience
    [Key]
    [Column("ID")]
    public int Id => Event?.Id ?? 0;
    public EventsModel Event { get; set; }
    public List<CommentsModel> Comments { get; set; } = new();
}