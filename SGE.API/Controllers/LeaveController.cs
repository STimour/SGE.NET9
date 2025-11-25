using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.Interfaces.Services;
using SGE.Application.DTOs.LeaveRequests;

namespace SGE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveController : ControllerBase
{
    private readonly ILeaveRequestService leaveService;
    private const int DefaultAnnualEntitlement = 25;

    public LeaveController(ILeaveRequestService leaveService)
    {
        this.leaveService = leaveService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestLeave([FromBody] LeaveRequestCreateDto dto, CancellationToken cancellationToken)
    {
        if (dto.EndDate.Date < dto.StartDate.Date)
            return BadRequest(new { message = "EndDate must be on or after StartDate" });

        var created = await leaveService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetBalance), new { employeeId = created.EmployeeId }, created);
    }

    [HttpGet("{employeeId:int}/balance")]
    public async Task<IActionResult> GetBalance(int employeeId, [FromQuery] int? year = null, CancellationToken cancellationToken = default)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var remaining = await leaveService.GetRemainingLeaveDaysAsync(employeeId, y, cancellationToken);
        var entitlement = DefaultAnnualEntitlement;
        var taken = entitlement - remaining;
        if (taken < 0) taken = 0;
        return Ok(new { employeeId, year = y, entitlement, taken, remaining });
    }
}
