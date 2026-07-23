using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    public int ThreadId { get; set; }

    public int SenderId { get; set; }

    [MaxLength(50)]
    public string? SenderRole { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(ThreadId))]
    public virtual ChatThread? Thread { get; set; }

    [ForeignKey(nameof(SenderId))]
    public virtual User? Sender { get; set; }
}