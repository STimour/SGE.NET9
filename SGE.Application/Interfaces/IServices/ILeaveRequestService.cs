using SGE.Application.DTOs.LeaveRequests;
using SGE.Core.Enums;

namespace SGE.Application.Interfaces.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync();
    Task<bool> UpdateStatusAsync(int id, LeaveRequestUpdateDto dto, CancellationToken cancellationToken = default);
    Task<int> GetRemainingLeaveDaysAsync(int employeeId, int year, CancellationToken cancellationToken = default);
    Task<bool> HasConflictingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeRequestId = null, CancellationToken cancellationToken = default);
}
