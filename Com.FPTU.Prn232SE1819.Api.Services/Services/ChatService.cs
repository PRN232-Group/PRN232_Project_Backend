using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class ChatService : IChatService
{
    private readonly InteriorStudioDbContext _context;

    public ChatService(InteriorStudioDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessageDto>> GetCustomerMessagesAsync(int customerId)
    {
        var thread = await _context.ChatThreads
            .FirstOrDefaultAsync(t => t.CustomerId == customerId);

        if (thread == null) return new List<ChatMessageDto>();

        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ThreadId == thread.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => MapToDto(m, customerId)).ToList();
    }

    public async Task<List<ChatCustomerDto>> GetChatCustomersAsync()
    {
        var threads = await _context.ChatThreads
            .Include(t => t.Customer)
            .Include(t => t.Messages)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        var result = new List<ChatCustomerDto>();

        foreach (var thread in threads)
        {
            if (thread.Customer == null) continue;

            var lastMsg = thread.Messages
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            result.Add(new ChatCustomerDto
            {
                CustomerId = thread.CustomerId,
                CustomerName = thread.Customer.FullName ?? thread.Customer.Email,
                CustomerEmail = thread.Customer.Email,
                LastMessage = lastMsg?.Content,
                LastMessageAt = lastMsg?.CreatedAt ?? thread.UpdatedAt
            });
        }

        return result;
    }

    public async Task<ChatMessageDto> SendMessageAsync(int senderId, string userRole, SendMessageDto dto)
    {
        int targetCustomerId;

        if (userRole.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            targetCustomerId = senderId;
        }
        else
        {
            if (!dto.CustomerId.HasValue)
            {
                throw new ArgumentException("Sales/Admin phải chỉ định customerId để gửi tin nhắn.");
            }
            targetCustomerId = dto.CustomerId.Value;
        }

        // Tự động tìm hoặc tạo Thread chat cho khách hàng
        var thread = await _context.ChatThreads
            .FirstOrDefaultAsync(t => t.CustomerId == targetCustomerId);

        var now = VnDateTime.Now;

        if (thread == null)
        {
            thread = new ChatThread
            {
                CustomerId = targetCustomerId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.ChatThreads.Add(thread);
            await _context.SaveChangesAsync();
        }
        else
        {
            thread.UpdatedAt = now;
        }

        var message = new ChatMessage
        {
            ThreadId = thread.Id,
            SenderId = senderId,
            SenderRole = userRole,
            Content = dto.Content,
            CreatedAt = now
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _context.Users.FindAsync(senderId);
        message.Sender = sender;

        return MapToDto(message, targetCustomerId);
    }

    private static ChatMessageDto MapToDto(ChatMessage m, int customerId) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderName = m.Sender?.FullName ?? m.Sender?.Email ?? string.Empty,
        CustomerId = customerId,
        Content = m.Content,
        SentAt = m.CreatedAt,
        IsFromCustomer = m.SenderId == customerId
    };
}