namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class EmployeeDto
    {
        public int BusinessEntityID { get; set; }
        public string NationalIDNumber { get; set; } = string.Empty;
        public string LoginID { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string MaritalStatus { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public bool SalariedFlag { get; set; }
        public short VacationHours { get; set; }
        public short SickLeaveHours { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}