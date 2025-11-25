using AutoMapper;
using SGE.Application.DTOs.Attendances;
using SGE.Application.Interfaces.Repositories;
using SGE.Core.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SGE.Application.DTO.Attendances;
using SGE.Application.Interfaces.IRepositories;
using SGE.Application.Interfaces.IServices;
using SGE.Core.Exceptions;

namespace SGE.Application.services;

/// <summary>
/// Provides functionalities related to managing employee attendances, including clocking in/out,
/// retrieving attendance records, and calculating worked hours for employees.
/// </summary>
public class AttendanceService( IAttendanceRepository attendanceRepository,
                                IEmployeeRepository employeeRepository,
                                IMapper mapper) : IAttendanceService
{
    
    /// <summary>
    /// Registers the clock-in time for an employee. If an attendance record for the employee on the specified day
    /// already exists without a clock-in time, it updates the record with the provided clock-in information.
    /// Otherwise, it creates a new attendance record.
    /// </summary>
    /// <param name="clockInDto">An object containing the employee's ID and clock-in time information.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>A DTO containing the attendance details after the clock-in operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the employee corresponding to the specified ID is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the employee has already clocked in on the same day or when there is an issue creating or updating the attendance record.
    /// </exception>
    public async Task<AttendanceDto> ClockInAsync(ClockInOutDto clockInDto, CancellationToken cancellationToken = default)
    {
        if (!await employeeRepository.ExistsAsync(clockInDto.EmployeeId, cancellationToken)) 
            throw new EmployeeNotFoundException(clockInDto.EmployeeId);

        var date = clockInDto.DateTime.Date;
        var time = clockInDto.DateTime.TimeOfDay;
        // TODO
        // Vérifier s'il existe déjà une entrée pour la date
        // Créer une nouvelle entrée de présence
        // Mettre à jour l'entrée existante
        var existing = (await attendanceRepository.FindAsync(a => a.EmployeeId == clockInDto.EmployeeId && a.Date == date, cancellationToken)).ToList();

        switch (existing.Count)
        {
            case > 1:
                throw new ValidationException($"{existing}", "Multiple attendance records found for the same employee and date" );
            case 1:
            {
                var attendance = existing.First();
                if (attendance.ClockIn.HasValue)
                    throw new ValidationException($"{attendance}","Employee has already clocked in today");

                attendance.ClockIn = time;
                attendance.Notes += (string.IsNullOrEmpty(attendance.Notes) ? "" : "; ") + clockInDto.Notes;
                attendance.UpdatedAt = DateTime.UtcNow;

                await attendanceRepository.UpdateAsync(attendance, cancellationToken);

                return mapper.Map<AttendanceDto>(attendance);
            }
        }

        var entity = new Attendance
        {
            EmployeeId = clockInDto.EmployeeId,
            Date = date,
            ClockIn = time,
            Notes = clockInDto.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await attendanceRepository.AddAsync(entity, cancellationToken);

        return mapper.Map<AttendanceDto>(entity);
    
    }

    /// <summary>
    /// Registers the clock-out time for an employee. It updates the existing attendance record for the employee
    /// on the specified day by adding the clock-out information. The record must already contain a clock-in time.
    /// </summary>
    /// <param name="clockOutDto">An object containing the employee's ID and clock-out time information.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>A DTO containing the updated attendance details after the clock-out operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the employee corresponding to the specified ID is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no clock-in record exists for the specified day, when the employee has not clocked in prior to clocking out,
    /// or when the employee has already clocked out on the same day.
    /// </exception>
    public async Task<AttendanceDto> ClockOutAsync(ClockInOutDto clockOutDto, CancellationToken cancellationToken = default)
    {
        // Vérifier que l'employé existe
        if (!await employeeRepository.ExistsAsync(clockOutDto.EmployeeId, cancellationToken)) throw new EmployeeNotFoundException(clockOutDto.EmployeeId);

        var date = clockOutDto.DateTime.Date;
        var time = clockOutDto.DateTime.TimeOfDay;
    
        var existing = (await attendanceRepository.FindAsync(a => a.EmployeeId == clockOutDto.EmployeeId && a.Date == date, cancellationToken)).ToList();

        if (existing.Count == 0)
            throw new InvalidOperationException("No clock-in record found for today");

        if (existing.Count > 1)
            throw new InvalidOperationException("Multiple attendance records found for the same employee and date");

        var attendance = existing.First();

        if (!attendance.ClockIn.HasValue) throw new InvalidOperationException("Employee must clock in before clocking out");
        if (attendance.ClockOut.HasValue) throw new InvalidOperationException("Employee has already clocked out today");

        attendance.ClockOut = time;
        attendance.Notes += (string.IsNullOrEmpty(attendance.Notes) ? "" : "; ") + clockOutDto.Notes;
        attendance.UpdatedAt = DateTime.UtcNow;

        // Calculer les heures travaillées
        CalculateWorkedHours(attendance);
        // Mettre à jour les informations de la présence
        await attendanceRepository.UpdateAsync(attendance, cancellationToken);

        return mapper.Map<AttendanceDto>(attendance);
    }

    /// <summary>
    /// Creates a new attendance record for an employee based on the provided details. The worked hours
    /// and overtime hours are calculated automatically during the creation process.
    /// </summary>
    /// <param name="createAttendanceDto">An object containing the details needed to create the attendance record, including clock-in/out times and break duration.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>A DTO containing the details of the newly created attendance record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided attendance creation object is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there is an issue during the creation of the attendance record or calculation of hours.</exception>
    public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto createAttendanceDto, CancellationToken cancellationToken = default)
    {
        if (createAttendanceDto == null) throw new ArgumentNullException(nameof(createAttendanceDto));

        if (!await employeeRepository.ExistsAsync(createAttendanceDto.EmployeeId, cancellationToken))
            throw new EmployeeNotFoundException(createAttendanceDto.EmployeeId);

        var date = createAttendanceDto.Date.Date;
        var exists = (await attendanceRepository.FindAsync(a => a.EmployeeId == createAttendanceDto.EmployeeId && a.Date == date, cancellationToken)).Any();
        if (exists) throw new InvalidOperationException($"Attendance record already exists for {date:yyyy-MM-dd}");

        var entity = mapper.Map<Attendance>(createAttendanceDto);
        // Map break hours to TimeSpan if mapping not configured
        entity.BreakDuration = TimeSpan.FromHours(createAttendanceDto.BreakDurationHours);
        entity.Date = date;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        // Calculer les heures travaillées si les deux heures sont fournies
        if (entity.ClockIn.HasValue && entity.ClockOut.HasValue)
            CalculateWorkedHours(entity);

        await attendanceRepository.AddAsync(entity, cancellationToken);

        return mapper.Map<AttendanceDto>(entity);
    }

    /// <summary>
    /// Retrieves an attendance record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the attendance record to retrieve.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>An object containing the attendance details, or null if no record is found with the specified ID.</returns>
    public async Task<AttendanceDto?> GetAttendanceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var att = await attendanceRepository.GetByIdAsync(id, cancellationToken);
        return att == null ? null : mapper.Map<AttendanceDto>(att);
    }

    /// <summary>
    /// Retrieves the attendance records for a specific employee within an optional date range.
    /// If no date range is provided, it returns all attendance records for the employee.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee for whom attendance records are being retrieved.</param>
    /// <param name="startDate">The start date of the range for filtering attendance records (optional).</param>
    /// <param name="endDate">The end date of the range for filtering attendance records (optional).</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>A collection of attendance records for the specified employee within the date range, if provided.</returns> 
    public async Task<IEnumerable<AttendanceDto>>
    GetAttendancesByEmployeeAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        if (!await employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new EmployeeNotFoundException(employeeId);

        var attendances = (await attendanceRepository.GetByEmployeeAsync(employeeId, cancellationToken)).AsEnumerable();

        if (startDate.HasValue)
            attendances = attendances.Where(a => a.Date >= startDate.Value.Date);
        if (endDate.HasValue)
            attendances = attendances.Where(a => a.Date <= endDate.Value.Date);

        return attendances.Select(a => mapper.Map<AttendanceDto>(a)).ToList();
    }
    
    /// <summary>
    /// Retrieves the attendance records for all employees on the specified date.
    /// </summary>
    /// <param name="date">The date for which to retrieve attendance records.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>A collection of attendance details for all employees on the specified date.</returns>
    /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
    public async Task<IEnumerable<AttendanceDto>>GetAttendancesByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var target = date.Date;
        var list = await attendanceRepository.FindAsync(a => a.Date == target, cancellationToken);
        return list.Select(a => mapper.Map<AttendanceDto>(a)).ToList();
    }
    
    /// <summary>
    /// Retrieves the attendance record for an employee for the current date, if available.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee whose attendance record is to be fetched.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>An attendance DTO containing the details of the employee's attendance for the current date, or null if no record exists.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no employee is found with the specified ID.</exception>
    /// <exception cref="InvalidOperationException">Thrown when multiple attendance records for the employee are found for the current date.</exception> 
    public async Task<AttendanceDto?> GetTodayAttendanceAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        if (!await employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new EmployeeNotFoundException(employeeId);

        var today = DateTime.Today;
        var list = (await attendanceRepository.FindAsync(a => a.EmployeeId == employeeId && a.Date == today, cancellationToken)).ToList();

        if (list.Count == 0) return null;
        if (list.Count > 1) throw new InvalidOperationException("Multiple attendance records found for today");

        return mapper.Map<AttendanceDto>(list.First());
    }
    
    /// <summary>
    /// Calculates the total hours worked by an employee for a specific month and year.
    /// Aggregates attendance records for the given employee within the specified month
    /// to compute the total worked hours.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="year">The year for which the worked hours are being calculated.</param>
    /// <param name="month">The month for which the worked hours are being calculated.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation for cancellation.</param>
    /// <returns>The total hours worked by the employee for the specified month and year.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no records are found for the specified employee.</exception>
    /// <exception cref="InvalidOperationException">Thrown if attendance data is incomplete or inconsistent for calculations.</exception>
    public async Task<decimal> GetMonthlyWorkedHoursAsync(int employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        if (!await employeeRepository.ExistsAsync(employeeId, cancellationToken))
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

        var attendances = (await attendanceRepository.GetByEmployeeAsync(employeeId, cancellationToken))
            .Where(a => a.Date.Year == year && a.Date.Month == month)
            .ToList();

        if (!attendances.Any())
            throw new KeyNotFoundException($"No attendance records found for employee {employeeId} for {year}-{month:D2}");

        // Somme des heures travaillées (ignorer valeurs nulles ou négatives)
        decimal total = attendances.Sum(a => Math.Max(0, a.WorkedHours));
        return total;
    }
    
    /// <summary>
    /// Calculates the total worked hours and overtime hours for the specified attendance record based on the clock-in and clock-out times.
    /// If a break duration is provided, it is subtracted from the total worked time.
    /// </summary>
    /// <param name="attendance">The attendance record containing the clock-in, clock-out, and break duration information to calculate worked hours.</param>
    private void CalculateWorkedHours(Attendance attendance)
    {
        if (!attendance.ClockIn.HasValue || !attendance.ClockOut.HasValue)
            return;
        
        var totalWorked = attendance.ClockOut.Value - attendance.ClockIn.Value;
        
        // Soustraire la pause
        if (attendance.BreakDuration.HasValue)
            totalWorked -= attendance.BreakDuration.Value;
        
        var workedHours = (decimal)totalWorked.TotalHours;
    
        // Calculer les heures normales (8 heures par jour)
        const decimal normalWorkingHours = 8m;
        
        if (workedHours <= normalWorkingHours)
        {
            attendance.WorkedHours = Math.Max(0, workedHours);
            attendance.OvertimeHours = 0;
        } else {
            attendance.WorkedHours = normalWorkingHours;
            attendance.OvertimeHours = workedHours - normalWorkingHours;
        }
    }
    
}