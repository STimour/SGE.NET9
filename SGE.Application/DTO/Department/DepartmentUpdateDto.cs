namespace SGE.Application.DTO.Department;

public class DepartmentUpdateDto
{
    /// <summary>
    /// Gets or sets the name of the department.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the department.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}