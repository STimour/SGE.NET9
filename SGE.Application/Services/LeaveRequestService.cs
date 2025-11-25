using AutoMapper;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.IRepositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Enums;

namespace SGE.Application.Services;

public class LeaveRequestService(
    IEmployeeRepository employeeRepository,
    ILeaveRequestRepository leaveRequestRepository,
    IMapper mapper)
    : ILeaveRequestService
{
    private readonly IEmployeeRepository employeeRepo = employeeRepository;
    private readonly ILeaveRequestRepository leaveRepo = leaveRequestRepository;
    private readonly IMapper map = mapper;

    public async Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!await employeeRepo.ExistsAsync(dto.EmployeeId, cancellationToken))
            throw new KeyNotFoundException($"Employee {dto.EmployeeId} not found");

        // check conflicts
        if (await HasConflictingLeaveAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, null, cancellationToken))
            throw new InvalidOperationException("Leave request conflicts with existing request");

        var entity = map.Map<LeaveRequest>(dto);
        // calculate days requested (business days)
        entity.DaysRequested = CalculateBusinessDays(dto.StartDate.Date, dto.EndDate.Date);
        entity.Status = LeaveStatus.Pending;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await leaveRepo.AddAsync(entity, cancellationToken);

        return map.Map<LeaveRequestDto>(entity);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var e = await leaveRepo.GetByIdAsync(id, cancellationToken);
        return e == null ? null : map.Map<LeaveRequestDto>(e);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var list = await leaveRepo.GetByEmployeeAsync(employeeId, cancellationToken);
        return list.Select(l => map.Map<LeaveRequestDto>(l)).ToList();
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default)
    {
        var list = await leaveRepo.FindAsync(l => l.Status == status, cancellationToken);
        return list.Select(l => map.Map<LeaveRequestDto>(l)).ToList();
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
    {
        var list = await leaveRepo.FindAsync(l => l.Status == LeaveStatus.Pending);
        return list.Select(l => map.Map<LeaveRequestDto>(l)).ToList();
    }

    public async Task<bool> UpdateStatusAsync(int id, LeaveRequestUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await leaveRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;

        entity.Status = dto.Status;
        entity.ManagerComments = dto.ManagerComments;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await leaveRepo.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<int> GetRemainingLeaveDaysAsync(int employeeId, int year, CancellationToken cancellationToken = default)
    {
        const int entitlement = 25;
        var taken = (await leaveRepo.FindAsync(l => l.EmployeeId == employeeId && l.Status == LeaveStatus.Approved && l.StartDate.Year == year, cancellationToken))
            .Sum(l => l.DaysRequested);

        var remaining = entitlement - taken;
        return remaining < 0 ? 0 : remaining;
    }

    public async Task<bool> HasConflictingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeRequestId = null, CancellationToken cancellationToken = default)
    {
        var list = await leaveRepo.FindAsync(l => l.EmployeeId == employeeId, cancellationToken);
        return list.Any(l => (excludeRequestId == null || l.Id != excludeRequestId)
            && l.StartDate.Date <= endDate.Date && l.EndDate.Date >= startDate.Date);
    }

    private int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate) return 0;
        int businessDays = 0;
        var current = startDate.Date;
        while (current <= endDate.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                businessDays++;
            current = current.AddDays(1);
        }
        return businessDays;
    }
}
