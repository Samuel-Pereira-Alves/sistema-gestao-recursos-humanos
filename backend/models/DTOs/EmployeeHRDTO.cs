namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class EmployeeHRDto
    {
        public int EmployeeId { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Gender { get; set; }
        public DateTime HireDate { get; set; }
        public int VacationHours { get; set; }
        public int SickLeaveHours { get; set; }

        public List<PayHistoryDto> PayHistories { get; set; } = new();
        public List<DepartmentHistoryDto> DepartmentHistories { get; set; } = new();
    }
}