using Com.FPTU.Prn232SE1819.Api.Application.Dtos;

namespace Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;

public interface IChatService
{
    Task<List<ChatMessageDto>> GetCustomerMessagesAsync(int customerId);
    Task<List<ChatCustomerDto>> GetChatCustomersAsync();
    Task<ChatMessageDto> SendMessageAsync(int senderId, string userRole, SendMessageDto dto);
}