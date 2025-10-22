namespace SGE.Application.DTO.Employee;

public class EmployeeDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public short DepartmentId { get; set; }
    public required string DepartmentName { get; set; }
}