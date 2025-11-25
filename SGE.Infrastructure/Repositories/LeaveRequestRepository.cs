using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories;
using SGE.Core.Entities;
using SGE.Infrastructure.Data;

namespace SGE.Infrastructure.Repositories;

public class LeaveRequestRepository : Repository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .Include(l => l.Employee)
            .ToListAsync(cancellationToken);
    }
}
