using Com.FPTU.Prn232SE1819.Api.Application.Dtos;
using Com.FPTU.Prn232SE1819.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Com.FPTU.Prn232SE1819.Api.WebApi.Controllers;

[ApiController]
[Route("api/systemlogs")]

public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _logs;

    public SystemLogsController(ISystemLogService logs) => _logs = logs;

    [HttpGet]
    public async Task<ActionResult<IList<SystemLogDto>>> GetAll([FromQuery] SystemLogQueryDto query)
        => Ok(await _logs.GetAllAsync(query));
}
