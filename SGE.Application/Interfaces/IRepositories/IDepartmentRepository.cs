using SGE.Core.Entities;

namespace SGE.Application.Interfaces.IRepositories;

    /// <summary>
    /// Represents a repository interface for managing <see cref="Departement"/> entities.
    /// Provides additional methods specific to operations on departments.
    /// Inherits from <see cref="IRepository{T}"/> where T is <see cref="Departement"/>.
    /// </summary>
    public interface IDepartmentRepository : IRepository<Departement>
    {

        /// <summary>
        /// Asynchronously retrieves a <see cref="Departement"/> entity by its name.
        /// </summary>
        /// <param name="name">The name of the department to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="Departement"/> entity if found; otherwise, null.</returns>
        Task<Departement?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a <see cref="Departement"/> entity by its unique code.
        /// </summary>
        /// <param name="code">The unique code of the department to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="Departement"/> entity if found; otherwise, null.</returns>
        Task<Departement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
