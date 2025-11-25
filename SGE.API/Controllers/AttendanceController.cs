using Microsoft.AspNetCore.Mvc;
using SGE.Application.Interfaces.IServices;
using SGE.Application.DTOs.Attendances;

namespace SGE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        this.attendanceService = attendanceService;
    }

    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInOutDto dto, CancellationToken cancellationToken)
    {
        var result = await attendanceService.ClockInAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetToday), new { employeeId = dto.EmployeeId }, result);
    }

    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut([FromBody] ClockInOutDto dto, CancellationToken cancellationToken)
    {
        var result = await attendanceService.ClockOutAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{employeeId:int}/monthly-hours")]
    public async Task<IActionResult> GetMonthlyHours(int employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var hours = await attendanceService.GetMonthlyWorkedHoursAsync(employeeId, year, month, cancellationToken);
        return Ok(new { employeeId, year, month, hours });
    }

    [HttpGet("{employeeId:int}/today")]
    public async Task<IActionResult> GetToday(int employeeId, CancellationToken cancellationToken)
    {
        var att = await attendanceService.GetTodayAttendanceAsync(employeeId, cancellationToken);
        if (att == null) return NotFound();
        return Ok(att);
    }
}
