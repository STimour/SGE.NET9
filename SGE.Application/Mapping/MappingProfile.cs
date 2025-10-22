using AutoMapper;
using SGE.Application.DTO.Employee;
using SGE.Core.Entities;

namespace SGE.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                $"{src.FirstName} {src.LastName}"))

            .ForMember(dest => dest.DepartmentName, opt =>
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                opt.MapFrom(src => src.Departments != null ? src.Departments.Name : string.Empty));
#pragma warning restore CS8602

        CreateMap<EmployeeCreateDto, Employee>();
        CreateMap<EmployeeUpdateDto, Employee>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // ignore nulls
        
    }
}