
using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Moq;

using sistema_gestao_recursos_humanos.backend.models;
using sistema_gestao_recursos_humanos.backend.models.dtos;

namespace sistema_gestao_recursos_humanos.Tests.Utils
{
    public static class MapperMockFactory
    {
        // ----------------- DepartmentHistory -----------------

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

        // ----------------- Employee -----------------

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

        // ----------------- Fábricas de mocks -----------------

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

            m.Setup(x => x.Map<NotificationDto>(It.IsAny<Notification>()))
             .Returns((Notification n) => n == null ? null! : new NotificationDto
             {
                 ID = n.ID,
                 // Message = n.Message, // se existir
             });

            m.Setup(x => x.Map<List<NotificationDto>>(It.IsAny<List<Notification>>()))
             .Returns((List<Notification> src) => src?.Select(n => new NotificationDto
             {
                 ID = n.ID,
                 // Message = n.Message,
             }).ToList() ?? new List<NotificationDto>());

            m.Setup(x => x.Map<Notification>(It.IsAny<NotificationDto>()))
             .Returns((NotificationDto d) => d == null ? null! : new Notification
             {
                 ID = d.ID,
                 // Message = d.Message,
             });

            return m;
        }

    }
}