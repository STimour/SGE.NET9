using SGE.Application.Interfaces.IRepositories;
using SGE.Core.Entities;
using SGE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SGE.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    /// <summary>
    /// Represents a repository for performing data access operations specific to the <see cref= "Employee"/> entity.
    /// Implements the <see cref="IEmployeeRepository"/> interface and extends <see cref="Repository{T}"/>.
    /// Provides additional methods for retrieving employee data based on specific criteria.
    /// </summary>
    public EmployeeRepository(ApplicationDbContext context) : base(context) { }
    
    /// <inheritdoc/>
    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<Employee?> GetWithDepartmentAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(e => e.Departments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetPagedAsync(int
        pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(e => e.LastName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
