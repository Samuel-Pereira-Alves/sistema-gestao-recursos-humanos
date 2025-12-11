namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
    }
}