using AutoMapper;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.IRepositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Exceptions;
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
        // validate employee exists and load
        var employee = await employeeRepo.GetByIdAsync(dto.EmployeeId, cancellationToken);
        if (employee is null)
            throw new EmployeeNotFoundException(dto.EmployeeId);

        // validate dates
        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new ValidationException("EndDate", "La date de fin doit être supérieure ou égale à la date de début.");

        if (dto.StartDate.Date < DateTime.Today)
            throw new ValidationException("StartDate", "La date de début doit être supérieure ou égale à la date du jour.");

        // calculate business days
        var daysRequested = CalculateBusinessDays(dto.StartDate.Date, dto.EndDate.Date);

        // check remaining allowance
        var remaining = await GetRemainingLeaveDaysAsync(dto.EmployeeId, dto.StartDate.Year, cancellationToken);
        if (daysRequested > remaining)
            throw new InsufficientLeaveDaysException(daysRequested, remaining);

        // check conflicts
        var hasConflict = await HasConflictingLeaveAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, null, cancellationToken);
        if (hasConflict)
            throw new ConflictingLeaveRequestException(dto.StartDate, dto.EndDate);

        var entity = map.Map<LeaveRequest>(dto);
        entity.DaysRequested = daysRequested;
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
