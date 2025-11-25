using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGE.Infrastructure.Data;
using SGE.Core.Entities;
using SGE.Core.Enums;

namespace SGE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveController : ControllerBase
{
    private readonly ApplicationDbContext db;
    private const int DefaultAnnualEntitlement = 25; // jours par an

    public LeaveController(ApplicationDbContext db)
    {
        this.db = db;
    }

    public record CreateLeaveRequestDto(int EmployeeId, string LeaveType, DateTime StartDate, DateTime EndDate, string? Reason);
    public record LeaveBalanceDto(int EmployeeId, int Year, int Entitlement, int Taken, int Remaining);

    [HttpPost("request")]
    public async Task<IActionResult> RequestLeave([FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        // Validate employee exists
        var employee = await db.Employees.FindAsync(new object[] { dto.EmployeeId }, cancellationToken);
        if (employee == null) return NotFound(new { message = $"Employee {dto.EmployeeId} not found" });

        // Parse leave type
        if (!Enum.TryParse<LeaveType>(dto.LeaveType, true, out var leaveType))
            return BadRequest(new { message = "Invalid leave type" });

        if (dto.EndDate.Date < dto.StartDate.Date)
            return BadRequest(new { message = "EndDate must be on or after StartDate" });

        var days = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;

        var leave = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            LeaveType = leaveType,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            DaysRequested = days,
            Reason = dto.Reason ?? string.Empty,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBalance), new { employeeId = dto.EmployeeId }, leave);
    }

    [HttpGet("{employeeId:int}/balance")]
    public async Task<IActionResult> GetBalance(int employeeId, [FromQuery] int? year = null, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees.FindAsync(new object[] { employeeId }, cancellationToken);
        if (employee == null) return NotFound(new { message = $"Employee {employeeId} not found" });

        var y = year ?? DateTime.UtcNow.Year;

        var taken = await db.LeaveRequests
            .Where(l => l.EmployeeId == employeeId && l.Status == LeaveStatus.Approved && l.StartDate.Year == y)
            .SumAsync(l => (int?)l.DaysRequested, cancellationToken) ?? 0;

        var entitlement = DefaultAnnualEntitlement;
        var remaining = entitlement - taken;
        if (remaining < 0) remaining = 0;

        var dto = new LeaveBalanceDto(employeeId, y, entitlement, taken, remaining);
        return Ok(dto);
    }
}
