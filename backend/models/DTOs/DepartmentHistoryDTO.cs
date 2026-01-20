namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class DepartmentHistoryDto
    {
        public int BusinessEntityID { get; set; }

        public int DepartmentId { get; set; }
        public byte ShiftID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Person? Person {get; set;}
        
        public DepartmentDto? Department { get; set; }
        public Department? Dep { get; set; }
    }
}