using AutoMapper;
using SGE.Application.DTO.Department;
using SGE.Application.DTO.Employee;
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
        // Department Mapping
        // =======================

        CreateMap<Department, DepartmentDto>();
        CreateMap<DepartmentCreateDto, Department>();
        CreateMap<DepartmentUpdateDto, Department>()
            .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}