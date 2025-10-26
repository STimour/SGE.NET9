namespace SGE.Application.DTO.Employee;

public class EmployeeDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the employee.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the employee.
    /// </summary>
    public required string FullName { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the employee.
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the phone number of the employee.
    /// </summary>
    public required string PhoneNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the position or job title of the employee.
    /// </summary>
    public string Position { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the salary of the employee.
    /// </summary>
    public decimal Salary { get; set; }
    
    /// <summary>
    /// Is currently employed by the company.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the department associated with the employee.
    /// </summary>
    public required string DepartmentName { get; set; }

    /// <summary>
    /// Gets or sets the seniority (in years) of the employee within the company.
    /// </summary>
    public int Seniority { get; set; }
}