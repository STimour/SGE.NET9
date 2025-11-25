using SGE.Core.Entities;

namespace SGE.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for LeaveRequest-specific operations.
/// </summary>
public interface ILeaveRequestRepository : IRepository<LeaveRequest>
{
    /// <summary>
    /// Retrieves leave requests for a specific employee.
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
}
