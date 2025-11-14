using SGE.Application.DTO.Import;

namespace SGE.Application.Interfaces.IServices;

public interface IImportService
{
    Task<ImportResultDto> ImportEmployeesAsync(System.IO.Stream fileStream, string fileName, ImportDuplicateBehavior behavior = ImportDuplicateBehavior.Update, CancellationToken cancellationToken = default);
    Task<ImportResultDto> ImportDepartmentsAsync(System.IO.Stream fileStream, string fileName, ImportDuplicateBehavior behavior = ImportDuplicateBehavior.Update, CancellationToken cancellationToken = default);
}
