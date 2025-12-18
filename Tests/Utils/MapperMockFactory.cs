
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using sistema_gestao_recursos_humanos.backend.controllers;
using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Utils
{
    public static class MapperMockFactory
    {
        private static DepartmentHistoryDto MapDepartmentHistoryToDto(DepartmentHistory s)
        {
            if (s == null) return null!;
            return new DepartmentHistoryDto
            {
                BusinessEntityID = s.BusinessEntityID,
                DepartmentId = s.DepartmentID,
                ShiftID = s.ShiftID,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
            };
        }

        private static DepartmentHistory MapDepartmentHistoryFromDto(DepartmentHistoryDto s)
        {
            if (s == null) return null!;
            return new DepartmentHistory
            {
                BusinessEntityID = s.BusinessEntityID,
                DepartmentID = (short)s.DepartmentId,
                ShiftID = s.ShiftID,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
            };
        }

        private static EmployeeDto MapEmployeeToDto(Employee e)
        {
            if (e == null) return null!;
            return new EmployeeDto
            {
                BusinessEntityID = e.BusinessEntityID,
                LoginID = e.LoginID,
                JobTitle = e.JobTitle,
                BirthDate = e.BirthDate,
                HireDate = e.HireDate,
                MaritalStatus = e.MaritalStatus,
                Gender = e.Gender,
                SalariedFlag = e.SalariedFlag,
                VacationHours = e.VacationHours,
                SickLeaveHours = e.SickLeaveHours,
                NationalIDNumber = e.NationalIDNumber,
                Person = e.Person != null ? new PersonDto
                {
                    FirstName = e.Person.FirstName,
                    LastName = e.Person.LastName,
                    MiddleName = e.Person.MiddleName,
                    Title = e.Person.Title,
                    Suffix = e.Person.Suffix
                } : null,
                PayHistories = e!.PayHistories!.Select(ph => new PayHistoryDto
                {
                    Rate = ph.Rate,
                    PayFrequency = ph.PayFrequency,
                    RateChangeDate = ph.RateChangeDate
                }).ToList(),
                DepartmentHistories = e!.DepartmentHistories!.Select(MapDepartmentHistoryToDto).ToList()
            };
        }

        private static void ApplyEmployeeDtoToEntity(EmployeeDto s, Employee d)
        {
            if (s == null || d == null) return;
            d.LoginID = s.LoginID ?? d.LoginID;
            d.JobTitle = s.JobTitle ?? d.JobTitle;
            d.Gender = s.Gender ?? d.Gender;
            d.MaritalStatus = s.MaritalStatus ?? d.MaritalStatus;
            d.NationalIDNumber = s.NationalIDNumber ?? d.NationalIDNumber;

            if (s.VacationHours != default) d.VacationHours = s.VacationHours;
            if (s.SickLeaveHours != default) d.SickLeaveHours = s.SickLeaveHours;
            if (s.SalariedFlag != default(bool)) d.SalariedFlag = s.SalariedFlag;

            if (s.HireDate != default) d.HireDate = s.HireDate;
            if (s.BirthDate != default) d.BirthDate = s.BirthDate;
        }

        private static Employee MapEmployeeFromDto(EmployeeDto s)
        {
            if (s == null) return null!;
            var e = new Employee
            {
                BusinessEntityID = s.BusinessEntityID,
                LoginID = s.LoginID,
                JobTitle = s.JobTitle,
                BirthDate = s.BirthDate,
                HireDate = s.HireDate,
                MaritalStatus = s.MaritalStatus,
                Gender = s.Gender,
                SalariedFlag = s.SalariedFlag,
                VacationHours = s.VacationHours,
                SickLeaveHours = s.SickLeaveHours,
                NationalIDNumber = s.NationalIDNumber,
            };
            if (s.Person != null)
            {
                e.Person = new Person
                {
                    BusinessEntityID = s.BusinessEntityID,
                    FirstName = s.Person.FirstName ?? "",
                    LastName = s.Person.LastName ?? "",
                    MiddleName = s.Person.MiddleName,
                    Title = s.Person.Title,
                    Suffix = s.Person.Suffix,
                    PersonType = "EM",
                    ModifiedDate = DateTime.UtcNow
                };
            }
            return e;
        }

        public static Mock<IMapper> CreateDepartmentHistoryMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<DepartmentHistoryDto>>(List<DepartmentHistory>)
            m.Setup(x => x.Map<List<DepartmentHistoryDto>>(It.IsAny<List<DepartmentHistory>>()))
             .Returns((List<DepartmentHistory> src) => src?.Select(MapDepartmentHistoryToDto).ToList() ?? new List<DepartmentHistoryDto>());

            // Map<DepartmentHistoryDto>(DepartmentHistory)
            m.Setup(x => x.Map<DepartmentHistoryDto>(It.IsAny<DepartmentHistory>()))
             .Returns((DepartmentHistory src) => MapDepartmentHistoryToDto(src));

            // Map<DepartmentHistory>(DepartmentHistoryDto)
            m.Setup(x => x.Map<DepartmentHistory>(It.IsAny<DepartmentHistoryDto>()))
             .Returns((DepartmentHistoryDto src) => MapDepartmentHistoryFromDto(src));

            // Map(source, destination) para DepartmentHistory
            m.Setup(x => x.Map(It.IsAny<DepartmentHistoryDto>(), It.IsAny<DepartmentHistory>()))
             .Returns((DepartmentHistoryDto src, DepartmentHistory dest) =>
             {
                 if (src != null && dest != null)
                 {
                     dest.DepartmentID = (short)(src.DepartmentId != default ? src.DepartmentId : dest.DepartmentID);
                     if (src.ShiftID != default) dest.ShiftID = src.ShiftID;
                     if (src.EndDate.HasValue) dest.EndDate = src.EndDate;
                     if (src.StartDate != default) dest.StartDate = src.StartDate;
                 }
                 return dest!;
             });

            return m;
        }

        public static Mock<IMapper> CreateEmployeeMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Employee → DTO
            m.Setup(x => x.Map<EmployeeDto>(It.IsAny<Employee>()))
             .Returns((Employee src) => MapEmployeeToDto(src));

            m.Setup(x => x.Map<List<EmployeeDto>>(It.IsAny<List<Employee>>()))
             .Returns((List<Employee> src) => src?.Select(MapEmployeeToDto).ToList() ?? new List<EmployeeDto>());

            // PayHistory → DTO list
            m.Setup(x => x.Map<List<PayHistoryDto>>(It.IsAny<List<PayHistory>>()))
             .Returns((List<PayHistory> src) => src?.Select(ph => new PayHistoryDto
             {
                 Rate = ph.Rate,
                 PayFrequency = ph.PayFrequency,
                 RateChangeDate = ph.RateChangeDate
             }).ToList() ?? new List<PayHistoryDto>());

            // DepartmentHistory → DTO list
            m.Setup(x => x.Map<List<DepartmentHistoryDto>>(It.IsAny<List<DepartmentHistory>>()))
             .Returns((List<DepartmentHistory> src) => src?.Select(MapDepartmentHistoryToDto).ToList() ?? new List<DepartmentHistoryDto>());

            // DTO → Employee
            m.Setup(x => x.Map<Employee>(It.IsAny<EmployeeDto>()))
             .Returns((EmployeeDto src) => MapEmployeeFromDto(src));

            // Map(source, destination) para Employee (PUT)
            m.Setup(x => x.Map(It.IsAny<EmployeeDto>(), It.IsAny<Employee>()))
             .Returns((EmployeeDto src, Employee dest) =>
             {
                 ApplyEmployeeDtoToEntity(src, dest);
                 return dest;
             });

            // DepartmentHistory single → DTO (às vezes usado indiretamente)
            m.Setup(x => x.Map<DepartmentHistoryDto>(It.IsAny<DepartmentHistory>()))
             .Returns((DepartmentHistory src) => MapDepartmentHistoryToDto(src));

            return m;
        }

        public static Mock<IMapper> CreateNotificationMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<NotificationDto>>(List<Notification>)
            m.Setup(x => x.Map<List<NotificationDto>>(It.IsAny<List<Notification>>()))
             .Returns((List<Notification> src) => src?.Select(n => new NotificationDto
             {
                 ID = n.ID,
                 BusinessEntityID = n.BusinessEntityID,
                 Message = n.Message
             }).ToList() ?? new List<NotificationDto>());

            // Map<NotificationDto>(Notification)
            m.Setup(x => x.Map<NotificationDto>(It.IsAny<Notification>()))
             .Returns((Notification n) => new NotificationDto
             {
                 ID = n.ID,
                 BusinessEntityID = n.BusinessEntityID,
                 Message = n.Message
             });

            // Map<Notification>(NotificationDto)
            m.Setup(x => x.Map<Notification>(It.IsAny<NotificationDto>()))
             .Returns((NotificationDto d) => new Notification
             {
                 ID = d.ID,
                 BusinessEntityID = d.BusinessEntityID,
                 Message = d.Message
             });

            return m;
        }

        public static Mock<IMapper> CreatePayHistoryMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<PayHistoryDto>>(List<PayHistory>)
            m.Setup(x => x.Map<List<PayHistoryDto>>(It.IsAny<List<PayHistory>>()))
             .Returns((List<PayHistory> src) => src?.Select(ph => new PayHistoryDto
             {
                 BusinessEntityID = ph.BusinessEntityID,
                 RateChangeDate = ph.RateChangeDate,
                 Rate = ph.Rate,
                 PayFrequency = ph.PayFrequency
             }).ToList() ?? new List<PayHistoryDto>());

            // Map<PayHistoryDto>(PayHistory)
            m.Setup(x => x.Map<PayHistoryDto>(It.IsAny<PayHistory>()))
             .Returns((PayHistory ph) => new PayHistoryDto
             {
                 BusinessEntityID = ph.BusinessEntityID,
                 RateChangeDate = ph.RateChangeDate,
                 Rate = ph.Rate,
                 PayFrequency = ph.PayFrequency
             });

            // Map<PayHistory>(PayHistoryDto)
            m.Setup(x => x.Map<PayHistory>(It.IsAny<PayHistoryDto>()))
             .Returns((PayHistoryDto dto) => new PayHistory
             {
                 BusinessEntityID = dto.BusinessEntityID,
                 RateChangeDate = dto.RateChangeDate,
                 Rate = dto.Rate,
                 PayFrequency = dto.PayFrequency
                 // ModifiedDate setado no controller
             });

            // Map(source, destination) → PUT
            m.Setup(x => x.Map(It.IsAny<PayHistoryDto>(), It.IsAny<PayHistory>()))
             .Returns((PayHistoryDto src, PayHistory dest) =>
             {
                 if (src != null && dest != null)
                 {
                     // NÃO alterar chave composta
                     if (src.Rate != default(decimal)) dest.Rate = src.Rate;
                     if (src.PayFrequency != default(byte)) dest.PayFrequency = src.PayFrequency;
                 }
                 return dest!;
             });

            return m;
        }

        public static Mock<IMapper> CreateJobCandidateMapperMock()
        {
            var m = new Mock<IMapper>(MockBehavior.Strict);

            // Map<List<JobCandidateDto>>(List<JobCandidate>)
            m.Setup(x => x.Map<List<JobCandidateDto>>(It.IsAny<List<JobCandidate>>()))
             .Returns((List<JobCandidate> src) =>
                 src?.Select(c => new JobCandidateDto
                 {
                     JobCandidateId = c.JobCandidateId,
                     Resume = c.Resume!,
                     CvFileUrl = c.CvFileUrl!,
                     ModifiedDate = c.ModifiedDate,
                     FirstName = c.FirstName,
                     LastName = c.LastName,
                     NationalIDNumber = c.NationalIDNumber,
                     BirthDate = c.BirthDate,
                     MaritalStatus = c.MaritalStatus,
                     Gender = c.Gender
                 }).ToList() ?? new List<JobCandidateDto>());

            // Map<JobCandidateDto>(JobCandidate)
            m.Setup(x => x.Map<JobCandidateDto>(It.IsAny<JobCandidate>()))
             .Returns((JobCandidate c) => new JobCandidateDto
             {
                 JobCandidateId = c.JobCandidateId,
                 Resume = c.Resume!,
                 CvFileUrl = c.CvFileUrl!,
                 ModifiedDate = c.ModifiedDate,
                 FirstName = c.FirstName,
                 LastName = c.LastName,
                 NationalIDNumber = c.NationalIDNumber,
                 BirthDate = c.BirthDate,
                 MaritalStatus = c.MaritalStatus,
                 Gender = c.Gender
             });

            // Map<JobCandidate>(JobCandidateDto)
            m.Setup(x => x.Map<JobCandidate>(It.IsAny<JobCandidateDto>()))
             .Returns((JobCandidateDto d) => new JobCandidate
             {
                 JobCandidateId = d.JobCandidateId,
                 Resume = d.Resume,
                 CvFileUrl = d.CvFileUrl,
                 ModifiedDate = d.ModifiedDate,
                 FirstName = d.FirstName,
                 LastName = d.LastName,
                 NationalIDNumber = d.NationalIDNumber,
                 BirthDate = d.BirthDate,
                 MaritalStatus = d.MaritalStatus,
                 Gender = d.Gender,
                 PasswordHash = "DevOnly!234",
                 Role = "employee"
             });

            return m;
        }

        public static Mock<IWebHostEnvironment> CreateEnvMock(string root)
        {
            Directory.CreateDirectory(root); 
            var env = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            env.SetupGet(x => x.WebRootPath).Returns(root);
            env.SetupGet(x => x.ContentRootPath).Returns(root);
            return env;
        }

        public static Mock<ILogger<JobCandidateController>> CreateLoggerMock()
        {
            return new Mock<ILogger<JobCandidateController>>(MockBehavior.Loose);
        }

        public static Mock<ILogger<NotificationController>> CreateLoggerMockNotification()
        {
            return new Mock<ILogger<NotificationController>>(MockBehavior.Loose);
        }
        public static Mock<ILogger<AuthController>> CreateLoggerMockAuth()
        {
            return new Mock<ILogger<AuthController>>(MockBehavior.Loose);
        }
        public static Mock<ILogger<DepartmentHistoryController>> CreateLoggerMockDepartment()
        {
            return new Mock<ILogger<DepartmentHistoryController>>(MockBehavior.Loose);
        }
        public static Mock<ILogger<EmployeeController>> CreateLoggerMockEmployee()
        {
            return new Mock<ILogger<EmployeeController>>(MockBehavior.Loose);
        }

        public static Mock<ILogger<PayHistoryController>> CreateLoggerMockPayHistory()
        {
            return new Mock<ILogger<PayHistoryController>>(MockBehavior.Loose);
        }

    }
}