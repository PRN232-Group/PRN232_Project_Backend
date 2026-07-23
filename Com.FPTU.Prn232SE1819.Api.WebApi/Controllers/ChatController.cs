using System.Security.Claims;
using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service)
    {
        _service = service;
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id") ?? "0");

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

    [HttpGet("messages")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyMessages()
    {
        var messages = await _service.GetCustomerMessagesAsync(GetCurrentUserId());
        return Ok(messages);
    }

    [HttpGet("customers")]
    [Authorize(Roles = "Sales,Admin")]
    public async Task<IActionResult> GetChatCustomers()
    {
        var customers = await _service.GetChatCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("messages/{customerId:int}")]
    [Authorize(Roles = "Sales,Admin")]
    public async Task<IActionResult> GetCustomerMessages(int customerId)
    {
        var messages = await _service.GetCustomerMessagesAsync(customerId);
        return Ok(messages);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var result = await _service.SendMessageAsync(GetCurrentUserId(), GetCurrentUserRole(), dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}