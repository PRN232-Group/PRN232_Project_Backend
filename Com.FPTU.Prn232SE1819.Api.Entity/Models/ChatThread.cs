using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

[Table("ChatThreads")]
public class ChatThread
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual User? Customer { get; set; }

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}