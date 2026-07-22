using System.ComponentModel.DataAnnotations;

namespace Com.FPTU.Prn232SE1819.Api.Application.Dtos;

public class SendMessageDto
{
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
    public string Content { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsFromCustomer { get; set; }
}

public class ChatCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
}