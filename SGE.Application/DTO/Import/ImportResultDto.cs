using System.Collections.Generic;

namespace SGE.Application.DTO.Import;

public class ImportResultDto
{
    public int Total { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<ImportRecordResultDto> Records { get; set; } = new();
}
