using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Com.FPTU.Prn232SE1819.Api.Entity.Models;
using Com.FPTU.Prn232SE1819.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Com.FPTU.Prn232SE1819.Api.Services.Services;

public class DesignRequestService : IDesignRequestService
{
    private readonly InteriorStudioDbContext _context;

    private static readonly Dictionary<string, string> ForwardStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "New", "InReview" },
        { "InReview", "Quoted" },
        { "Quoted", "Done" }
    };

    public DesignRequestService(InteriorStudioDbContext context)
    {
        _context = context;
    }

    public async Task<DesignRequestDto> CreateAsync(int customerId, CreateDesignRequestDto dto)
    {
        var designRequest = new DesignRequest
        {
            CustomerId = customerId,
            Title = dto.Title,
            Style = dto.Style,
            InteriorDesignId = dto.InteriorDesignId,
            Budget = dto.Budget,
            Notes = dto.Notes,
            Status = "New",
            CreatedAt = VnDateTime.Now
        };

        if (dto.RelatedProductIds != null && dto.RelatedProductIds.Count != 0)
        {
            foreach (var prodId in dto.RelatedProductIds.Distinct())
            {
                designRequest.Products.Add(new DesignRequestProduct { ProductId = prodId });
            }
        }

        if (dto.Attachments != null && dto.Attachments.Count != 0)
        {
            foreach (var url in dto.Attachments)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    designRequest.Attachments.Add(new DesignRequestAttachment { FileUrl = url });
                }
            }
        }

        _context.DesignRequests.Add(designRequest);
        await _context.SaveChangesAsync();

        return await GetByIdInternalAsync(designRequest.Id)
            ?? throw new Exception("Lỗi khởi tạo Yêu cầu thiết kế.");
    }

    public async Task<List<DesignRequestDto>> GetAllAsync()
    {
        var list = await _context.DesignRequests
            .Include(x => x.Customer)
            .Include(x => x.Products)
            .Include(x => x.Attachments)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<List<DesignRequestDto>> GetMineAsync(int customerId)
    {
        var list = await _context.DesignRequests
            .Include(x => x.Customer)
            .Include(x => x.Products)
            .Include(x => x.Attachments)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<DesignRequestDto?> GetByIdAsync(int id, int currentUserId, string role)
    {
        var req = await GetByIdInternalAsync(id);
        if (req == null) return null;

        if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase) && req.CustomerId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập yêu cầu thiết kế này.");
        }

        return req;
    }

    public async Task<DesignRequestDto> UpdateStatusAsync(int id, string newStatus)
    {
        var req = await _context.DesignRequests.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy Yêu cầu thiết kế.");

        if (!ForwardStatusMap.TryGetValue(req.Status, out var expectedNextStatus) ||
            !expectedNextStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Chuyển trạng thái không hợp lệ. Trạng thái hiện tại: '{req.Status}', chỉ được chuyển sang '{expectedNextStatus}'.");
        }

        req.Status = expectedNextStatus;
        await _context.SaveChangesAsync();

        return (await GetByIdInternalAsync(id))!;
    }

    private async Task<DesignRequestDto?> GetByIdInternalAsync(int id)
    {
        var x = await _context.DesignRequests
            .Include(r => r.Customer)
            .Include(r => r.Products)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id);

        return x == null ? null : MapToDto(x);
    }

    private static DesignRequestDto MapToDto(DesignRequest x) => new()
    {
        Id = x.Id,
        CustomerId = x.CustomerId,
        CustomerName = x.Customer?.FullName ?? x.Customer?.Email ?? string.Empty,
        Title = x.Title,
        Style = x.Style,
        InteriorDesignId = x.InteriorDesignId,
        Budget = x.Budget,
        Notes = x.Notes,
        Status = x.Status,
        CreatedAt = x.CreatedAt,
        RelatedProductIds = x.Products.Select(p => p.ProductId).ToList(),
        Attachments = x.Attachments.Select(a => a.FileUrl).ToList()
    };
}