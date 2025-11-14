namespace SGE.Application.DTO.Import;

public class ImportRecordResultDto
{
    public int RowNumber { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RawData { get; set; }
}
