using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissions;
    private readonly IRoleService _roles;

    public PermissionsController(IPermissionService permissions, IRoleService roles)
    {
        _permissions = permissions;
        _roles = roles;
    }

    /// <summary>Quyền của user đang đăng nhập (pageKeys).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyPermissionsDto>> Mine()
    {
        if (!int.TryParse(User.FindFirstValue("roleId"), out var roleId))
        {
            var roleName = User.FindFirstValue(ClaimTypes.Role);
            var all = await _roles.GetAllAsync();
            var role = all.FirstOrDefault(r =>
                string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));
            if (role == null) return Ok(new MyPermissionsDto());
            roleId = role.Id;
        }

        var keys = await _permissions.GetPageKeysForRoleAsync(roleId);
        return Ok(new MyPermissionsDto { PageKeys = keys.ToList() });
    }

    [HttpGet("matrix")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PermissionMatrixDto>> Matrix()
        => Ok(await _permissions.GetMatrixAsync());

    [HttpPut("roles/{roleId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(int roleId, [FromBody] SetRolePermissionsDto dto)
    {
        try
        {
            await _permissions.SetRolePermissionsAsync(roleId, dto);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
