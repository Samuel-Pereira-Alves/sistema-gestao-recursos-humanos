namespace sistema_gestao_recursos_humanos.backend.models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public long NationalIdNumber { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Gender { get; set; }
        public DateTime HireDate { get; set; }
        public bool SalariedFlag { get; set; }
        public int VacationHours { get; set; }
        public int SickLeaveHours { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Relações
        public List<DepartmentHistory>? DepartmentHistories { get; set; }
        public List<PayHistory>? PayHistories { get; set; }
    }
}