using AutoMapper;
using SGE.Application.DTO.Department;
using SGE.Application.DTO.Employee;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Core.Entities;

namespace SGE.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // =======================
        // Employee Mapping
        // =======================

        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.DepartmentName, opt =>
                opt.MapFrom(src => src.Departments != null ? src.Departments.Name : string.Empty));
        CreateMap<EmployeeCreateDto, Employee>();
        CreateMap<EmployeeUpdateDto, Employee>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember)
                => srcMember != null)); // ignore nulls

        // =======================
        // Attendance Mapping (already added elsewhere)
        // =======================

        // =======================
        // LeaveRequest Mapping
        // =======================
        CreateMap<LeaveRequestCreateDto, LeaveRequest>();
        CreateMap<LeaveRequest, LeaveRequestDto>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"))
            .ForMember(dest => dest.LeaveTypeName, opt => opt.MapFrom(src => src.LeaveType.ToString()))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()));

        // =======================
        // Department Mapping
        // =======================

        CreateMap<Department, DepartmentDto>();
        CreateMap<DepartmentCreateDto, Department>();
        CreateMap<DepartmentUpdateDto, Department>()
            .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}