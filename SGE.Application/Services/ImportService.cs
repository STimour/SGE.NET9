using System.Globalization;
using AutoMapper;
using CsvHelper;
using ClosedXML.Excel;
using SGE.Application.DTO.Department;
using SGE.Application.DTO.Employee;
using SGE.Application.DTO.Import;
using SGE.Application.Interfaces.IRepositories;
using SGE.Application.Interfaces.IServices;
using SGE.Core.Entities;

namespace SGE.Application.Services;

public class ImportService : IImportService
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IDepartmentRepository departmentRepository;
    private readonly IMapper mapper;

    public ImportService(IEmployeeRepository employeeRepository,
                         IDepartmentRepository departmentRepository,
                         IMapper mapper)
    {
        this.employeeRepository = employeeRepository;
        this.departmentRepository = departmentRepository;
        this.mapper = mapper;
    }

    private static readonly Dictionary<string,string[]> EmployeeHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "FirstName", new[] { "firstname", "first_name", "givenname", "given_name", "nom", "Nom" } },
        { "LastName", new[] { "lastname", "last_name", "surname", "Lastname", "prénome", "prenom", "Prenom", "Prénom" } },
        { "Email", new[] { "email", "emailaddress", "email address", "courriel", "mail" } },
        { "PhoneNumber", new[] { "phonenumber", "phone", "phone_number", "numero", "numéro" } },
        { "Address", new[] { "address", "addr", "adress" } },
        { "Position", new[] { "position", "jobtitle", "title", "poste" } },
        { "Salary", new[] { "salary", "wage", "salaire" } },
        { "HireDate", new[] { "hiredate", "hire_date", "startdate", "start_date", "date d'embauche", "dateEmbauche" } },
        { "IsActive", new[] { "isactive", "active", "is_active", "travaille" } },
        { "DepartmentId", new[] { "departmentid", "department_id", "deptid", "dep_id", "id_department", "idDepartement", "departementId" } },
        { "DepartmentCode", new[] { "departmentcode", "department_code", "deptcode", "dept_code", "codeDepartement", "departementCode" } },
        { "DepartmentName", new[] { "departmentname", "department_name", "deptname", "nomDepartement", "nom du departement" } }
    };

    private static readonly Dictionary<string,string[]> DepartmentHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Name", new[] { "name", "departmentname", "department_name", "deptname", "nomDepartement", "nom du departement"  } },
        { "Code", new[] { "code", "departmentcode", "department_code", "deptcode", "dept_code", "codeDepartement", "departementCode" } },
        { "Description", new[] { "description", "desc" } }
    };

    public async Task<ImportResultDto> ImportEmployeesAsync(Stream fileStream, 
                                                                string fileName, 
                                                                ImportDuplicateBehavior behavior = ImportDuplicateBehavior.Update, 
                                                                CancellationToken cancellationToken = default)
        {
            var result = new ImportResultDto();
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
    
            if (ext == ".csv")
            {
                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();
                var headerRecord = csv.HeaderRecord ?? Array.Empty<string>();
                var headerMap = BuildHeaderMap(headerRecord, EmployeeHeaderAliases);
                var row = 1;
    
                while (csv.Read())
                {
                    row++;
                    result.Total++;
                    try
                    {
                        string GetField(string canonical)
                        {
                            if (headerMap.TryGetValue(canonical, out var name) && !string.IsNullOrEmpty(name))
                            {
                                return csv.GetField(name) ?? string.Empty;
                            }
                            return string.Empty;
                        }
    
                        var dto = new EmployeeCreateDto
                        {
                            FirstName = GetField("FirstName"),
                            LastName = GetField("LastName"),
                            Email = GetField("Email"),
                            PhoneNumber = GetField("PhoneNumber"),
                            Address = GetField("Address"),
                            Position = GetField("Position"),
                            Salary = decimal.TryParse(GetField("Salary"), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : 0m,
                            HireDate = DateTime.TryParse(GetField("HireDate"), out var d) ? d : DateTime.MinValue,
                            IsActive = bool.TryParse(GetField("IsActive"), out var b) ? b : true,
                            DepartmentId = int.TryParse(GetField("DepartmentId"), out var did) ? (int?)did : null,
                            DepartmentName = GetField("DepartmentName"),
                            DepartmentCode = GetField("DepartmentCode")
                        };
    
                        if (string.IsNullOrWhiteSpace(dto.Email))
                        {
                            result.Failed++;
                            result.Records.Add(new ImportRecordResultDto {
                                RowNumber = row, Success = false,
                                Message = "Missing email",
                                RawData = csv.Parser.RawRecord ?? string.Empty
                            });
                            continue;
                        }
    
                        var existing = await employeeRepository.GetByEmailAsync(dto.Email, cancellationToken);
                        if (existing != null)
                        {
                            if (behavior == ImportDuplicateBehavior.Skip)
                            {
                                result.Skipped++;
                                result.Records.Add(new ImportRecordResultDto {
                                    RowNumber = row, Success = true,
                                    Message = "Skipped",
                                    RawData = csv.Parser.RawRecord ?? string.Empty
                                });
                            }
                            else
                            {
                                await EnsureAndMapDepartmentAsync(dto, existing, departmentRepository, mapper, cancellationToken);
                                mapper.Map(dto, existing);
                                existing.UpdatedAt = DateTime.UtcNow;
                                await employeeRepository.UpdateAsync(existing, cancellationToken);
                                result.Updated++;
                                result.Records.Add(new ImportRecordResultDto {
                                    RowNumber = row, Success = true, Message = "Updated",
                                    RawData = csv.Parser.RawRecord ?? string.Empty
                                });
                            }
                        }
                        else
                        {
                            int assignedDeptId = await ResolveOrCreateDepartmentForDtoAsync(dto, departmentRepository, mapper, cancellationToken);
                            if (assignedDeptId > 0) dto.DepartmentId = assignedDeptId;
    
                            var entity = mapper.Map<Employee>(dto);
                            entity.CreatedAt = DateTime.UtcNow;
                            entity.UpdatedAt = DateTime.UtcNow;
                            await employeeRepository.AddAsync(entity, cancellationToken);
                            result.Created++;
                            result.Records.Add(new ImportRecordResultDto {
                                RowNumber = row, Success = true,
                                Message = "Created",
                                RawData = csv.Parser.RawRecord ?? string.Empty
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = false, Message = ex.Message });
                    }
                }
            }
            else if (ext == ".xlsx" || ext == ".xls")
            {
                using var workbook = new XLWorkbook(fileStream);
                var ws = workbook.Worksheets.First();
                var headerRow = ws.Row(1);
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    
                var lastHeaderCell = headerRow.LastCellUsed();
                if (lastHeaderCell != null)
                {
                    for (var c = 1; c <= lastHeaderCell.Address.ColumnNumber; c++)
                    {
                        var h = headerRow.Cell(c).GetString().Trim();
                        if (!string.IsNullOrEmpty(h) && !headers.ContainsKey(h)) headers[h] = c;
                    }
                }
    
                var headerMap = BuildHeaderMap(headers.Keys.ToArray(), EmployeeHeaderAliases);
    
                int row = 1;
                var lastRowUsed = ws.LastRowUsed();
                if (lastRowUsed != null)
                {
                    for (int r = 2; r <= lastRowUsed.RowNumber(); r++)
                    {
                        row = r;
                        result.Total++;
                        try
                        {
                            string GetByCanonical(string canonical)
                                => headerMap.TryGetValue(canonical, out var name) && 
                                   headers.TryGetValue(name, out var idx) 
                                   ? ws.Row(r).Cell(idx).GetString() 
                                   : string.Empty;
    
                            var dto = new EmployeeCreateDto
                            {
                                FirstName = GetByCanonical("FirstName"),
                                LastName = GetByCanonical("LastName"),
                                Email = GetByCanonical("Email"),
                                PhoneNumber = GetByCanonical("PhoneNumber"),
                                Address = GetByCanonical("Address"),
                                Position = GetByCanonical("Position"),
                                Salary = decimal.TryParse(GetByCanonical("Salary"), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : 0m,
                                HireDate = DateTime.TryParse(GetByCanonical("HireDate"), out var d) ? d : DateTime.MinValue,
                                IsActive = bool.TryParse(GetByCanonical("IsActive"), out var b) ? b : true,
                                DepartmentId = int.TryParse(GetByCanonical("DepartmentId"), out var did) ? (int?)did : null,
                                DepartmentName = GetByCanonical("DepartmentName"),
                                DepartmentCode = GetByCanonical("DepartmentCode")
                            };
    
                            if (string.IsNullOrWhiteSpace(dto.Email))
                            {
                                result.Failed++;
                                result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = false, Message = "Missing email" });
                                continue;
                            }
    
                            var existing = await employeeRepository.GetByEmailAsync(dto.Email, cancellationToken);
                            if (existing != null)
                            {
                                if (behavior == ImportDuplicateBehavior.Skip)
                                {
                                    result.Skipped++;
                                    result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = true, Message = "Skipped" });
                                }
                                else
                                {
                                    await EnsureAndMapDepartmentAsync(dto, existing, departmentRepository, mapper, cancellationToken);
                                    mapper.Map(dto, existing);
                                    existing.UpdatedAt = DateTime.UtcNow;
                                    await employeeRepository.UpdateAsync(existing, cancellationToken);
                                    result.Updated++;
                                    result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = true, Message = "Updated" });
                                }
                            }
                            else
                            {
                                var assignedDeptId = await ResolveOrCreateDepartmentForDtoAsync(dto, departmentRepository, mapper, cancellationToken);
                                if (assignedDeptId > 0) dto.DepartmentId = assignedDeptId;
    
                                var entity = mapper.Map<Employee>(dto);
                                entity.CreatedAt = DateTime.UtcNow;
                                entity.UpdatedAt = DateTime.UtcNow;
                                await employeeRepository.AddAsync(entity, cancellationToken);
                                result.Created++;
                                result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = true, Message = "Created" });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Failed++;
                            result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = false, Message = ex.Message });
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Only .csv and .xlsx/.xls files are supported");
            }
    
            return result;
        }

    private static Dictionary<string, string> BuildHeaderMap(string[] headers, Dictionary<string,string[]> aliases)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in aliases)
        {
            var canonical = kv.Key;

            var found = headers.FirstOrDefault(h => string.Equals(h, canonical, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(found)) 
            { 
                map[canonical] = found;
                continue;
            }

            var aliasFound = kv.Value
                .Select(a => headers.FirstOrDefault(h => string.Equals(h, a, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(h => !string.IsNullOrEmpty(h));

            map[canonical] = aliasFound ?? string.Empty;
        }

        return map;
    }

    private async Task<int> ResolveOrCreateDepartmentForDtoAsync(EmployeeCreateDto dto, IDepartmentRepository deptRepo, IMapper mapper, CancellationToken cancellationToken)
    {
        if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
        {
            var dep = await deptRepo.GetByIdAsync(dto.DepartmentId.Value, cancellationToken);
            if (dep != null) return dep.Id;

            var create = new DepartmentCreateDto
            {
                Name = string.IsNullOrWhiteSpace(dto.DepartmentName) ? $"Imported Dept {dto.DepartmentId.Value}" : dto.DepartmentName,
                Code = string.IsNullOrWhiteSpace(dto.DepartmentCode) ? $"IMP-{dto.DepartmentId.Value}" : dto.DepartmentCode,
                Description = string.Empty
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            return entity.Id;
        }

        if (!string.IsNullOrWhiteSpace(dto.DepartmentCode))
        {
            var byCode = (await deptRepo.FindAsync(d => d.Code == dto.DepartmentCode, cancellationToken)).FirstOrDefault();
            if (byCode != null) return byCode.Id;

            var create = new DepartmentCreateDto 
            { 
                Name = dto.DepartmentName ?? dto.DepartmentCode, 
                Code = dto.DepartmentCode, 
                Description = string.Empty 
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow; 
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            return entity.Id;
        }

        if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            var byName = (await deptRepo.FindAsync(d => d.Name == dto.DepartmentName, cancellationToken)).FirstOrDefault();
            if (byName != null) return byName.Id;

            var create = new DepartmentCreateDto 
            { 
                Name = dto.DepartmentName, 
                Code = dto.DepartmentCode ?? dto.DepartmentName, 
                Description = string.Empty 
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow; 
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            return entity.Id;
        }

        return 0;
    }

    private async Task EnsureAndMapDepartmentAsync(EmployeeCreateDto dto, Employee existing, IDepartmentRepository deptRepo, IMapper mapper, CancellationToken cancellationToken)
    {
        if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
        {
            var dep = await deptRepo.GetByIdAsync(dto.DepartmentId.Value, cancellationToken);
            if (dep != null) { existing.DepartmentId = dep.Id; return; }

            var create = new DepartmentCreateDto
            {
                Name = dto.DepartmentName ?? $"Imported Dept {dto.DepartmentId.Value}",
                Code = dto.DepartmentCode ?? $"IMP-{dto.DepartmentId.Value}",
                Description = string.Empty
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            existing.DepartmentId = entity.Id;
            return;
        }

        if (!string.IsNullOrWhiteSpace(dto.DepartmentCode))
        {
            var byCode = (await deptRepo.FindAsync(d => d.Code == dto.DepartmentCode, cancellationToken)).FirstOrDefault();
            if (byCode != null) { existing.DepartmentId = byCode.Id; return; }

            var create = new DepartmentCreateDto 
            { 
                Name = dto.DepartmentName ?? dto.DepartmentCode, 
                Code = dto.DepartmentCode, 
                Description = string.Empty 
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow; 
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            existing.DepartmentId = entity.Id;
            return;
        }

        if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            var byName = (await deptRepo.FindAsync(d => d.Name == dto.DepartmentName, cancellationToken)).FirstOrDefault();
            if (byName != null) { existing.DepartmentId = byName.Id; return; }

            var create = new DepartmentCreateDto 
            { 
                Name = dto.DepartmentName, 
                Code = dto.DepartmentCode ?? dto.DepartmentName, 
                Description = string.Empty 
            };

            var entity = mapper.Map<Department>(create);
            entity.CreatedAt = DateTime.UtcNow; 
            entity.UpdatedAt = DateTime.UtcNow;
            await deptRepo.AddAsync(entity, cancellationToken);
            existing.DepartmentId = entity.Id;
            return;
        }
    }

    public async Task<ImportResultDto> ImportDepartmentsAsync(Stream fileStream, string fileName, ImportDuplicateBehavior behavior = ImportDuplicateBehavior.Update, CancellationToken cancellationToken = default)
    {
        var result = new ImportResultDto();
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

        if (ext == ".csv")
        {
            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read(); 
            csv.ReadHeader(); 
            int row = 1;

            var headerRecord = csv.HeaderRecord ?? Array.Empty<string>();
            var headerMap = BuildHeaderMap(headerRecord, DepartmentHeaderAliases);

            while (csv.Read())
            {
                row++;
                result.Total++;
                try
                {
                    string GetField(string canonical)
                        => headerMap.TryGetValue(canonical, out var name) &&
                           !string.IsNullOrEmpty(name)
                           ? csv.GetField(name) ?? string.Empty
                           : string.Empty;

                    var dto = new DepartmentCreateDto
                    {
                        Name = GetField("Name"),
                        Code = GetField("Code"),
                        Description = GetField("Description")
                    };

                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        result.Failed++;
                        result.Records.Add(new ImportRecordResultDto {
                            RowNumber = row, Success = false, Message = "Missing name",
                            RawData = csv.Parser.RawRecord ?? string.Empty
                        });
                        continue;
                    }

                    var existing = (await departmentRepository.FindAsync(d => d.Code == dto.Code || d.Name == dto.Name, cancellationToken))
                        .FirstOrDefault();

                    if (existing != null)
                    {
                        if (behavior == ImportDuplicateBehavior.Skip)
                        {
                            result.Skipped++;
                            result.Records.Add(new ImportRecordResultDto {
                                RowNumber = row, Success = true, Message = "Skipped",
                                RawData = csv.Parser.RawRecord ?? string.Empty
                            });
                        }
                        else
                        {
                            mapper.Map(dto, existing);
                            existing.UpdatedAt = DateTime.UtcNow;
                            await departmentRepository.UpdateAsync(existing, cancellationToken);
                            result.Updated++;
                            result.Records.Add(new ImportRecordResultDto {
                                RowNumber = row, Success = true, Message = "Updated",
                                RawData = csv.Parser.RawRecord ?? string.Empty
                            });
                        }
                    }
                    else
                    {
                        var entity = mapper.Map<Department>(dto);
                        entity.CreatedAt = DateTime.UtcNow;
                        entity.UpdatedAt = DateTime.UtcNow;
                        await departmentRepository.AddAsync(entity, cancellationToken);
                        result.Created++;
                        result.Records.Add(new ImportRecordResultDto {
                            RowNumber = row, Success = true, Message = "Created",
                            RawData = csv.Parser.RawRecord ?? string.Empty
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Records.Add(new ImportRecordResultDto { RowNumber = row, Success = false, Message = ex.Message });
                }
            }
        }
        else if (ext == ".xlsx" || ext == ".xls")
        {
            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.First();
            var headerRow = ws.Row(1);
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var lastHeaderCell2 = headerRow.LastCellUsed();
            if (lastHeaderCell2 != null)
            {
                for (int c = 1; c <= lastHeaderCell2.Address.ColumnNumber; c++)
                {
                    var h = headerRow.Cell(c).GetString().Trim();
                    if (!string.IsNullOrEmpty(h) && !headers.ContainsKey(h)) headers[h] = c;
                }
            }

            var headerMap = BuildHeaderMap(headers.Keys.ToArray(), DepartmentHeaderAliases);

            var lastRowUsed2 = ws.LastRowUsed();
            if (lastRowUsed2 != null)
            {
                for (int r = 2; r <= lastRowUsed2.RowNumber(); r++)
                {
                result.Total++;
                try
                {
                    string GetByCanonical(string canonical)
                        => headerMap.TryGetValue(canonical, out var name) &&
                           headers.TryGetValue(name, out var idx)
                           ? ws.Row(r).Cell(idx).GetString()
                           : string.Empty;

                    var dto = new DepartmentCreateDto
                    {
                        Name = GetByCanonical("Name"),
                        Code = GetByCanonical("Code"),
                        Description = GetByCanonical("Description")
                    };

                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        result.Failed++;
                        result.Records.Add(new ImportRecordResultDto { RowNumber = r, Success = false, Message = "Missing name" });
                        continue;
                    }

                    var existing = (await departmentRepository.FindAsync(d => d.Code == dto.Code || d.Name == dto.Name, cancellationToken))
                        .FirstOrDefault();

                    if (existing != null)
                    {
                        mapper.Map(dto, existing);
                        existing.UpdatedAt = DateTime.UtcNow;
                        await departmentRepository.UpdateAsync(existing, cancellationToken);
                        result.Updated++;
                        result.Records.Add(new ImportRecordResultDto { RowNumber = r, Success = true, Message = "Updated" });
                    }
                    else
                    {
                        var entity = mapper.Map<Department>(dto);
                        entity.CreatedAt = DateTime.UtcNow;
                        entity.UpdatedAt = DateTime.UtcNow;
                        await departmentRepository.AddAsync(entity, cancellationToken);
                        result.Created++;
                        result.Records.Add(new ImportRecordResultDto { RowNumber = r, Success = true, Message = "Created" });
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Records.Add(new ImportRecordResultDto { RowNumber = r, Success = false, Message = ex.Message });
                }
            }
        }
        else
        {
            throw new NotSupportedException("Only .csv and .xlsx/.xls files are supported");
        }

        return result;
    }
    }
}