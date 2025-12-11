namespace sistema_gestao_recursos_humanos.backend.models
{
    public class DepartmentHistory
    {
        public int DepartmentHistoryId { get; set; }
        public int EmployeeId { get; set; }
        public int DepartmentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        //public Employee Employee { get; set; }
    }
}