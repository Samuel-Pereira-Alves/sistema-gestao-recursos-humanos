using AutoMapper;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.backend.profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeDto>().ReverseMap();
            CreateMap<PayHistory, PayHistoryDto>().ReverseMap();
            CreateMap<DepartmentHistory, DepartmentHistoryDto>().ReverseMap();

            CreateMap<Person, PersonDto>().ReverseMap();
            CreateMap<Department, DepartmentDto>().ReverseMap();
            CreateMap<JobCandidate, JobCandidateDto>().ReverseMap();
            CreateMap<Notification, NotificationDto>().ReverseMap();
            CreateMap<EmailRequest, EmailRequestDTO>().ReverseMap();
        }
    }
}