using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

/// <summary>Chỉ đọc danh sách role (không thêm/sửa/xóa). Phân quyền trang → /api/permissions.</summary>
[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        var list = await _roles.GetAllAsync();
        return Ok(list
            .Where(r => r.Name != "Production")
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
            }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetOne(int id)
    {
        var role = await _roles.GetOneAsync(id);
        if (role == null || role.Name == "Production") return NotFound();
        return Ok(new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
        });
    }
}
