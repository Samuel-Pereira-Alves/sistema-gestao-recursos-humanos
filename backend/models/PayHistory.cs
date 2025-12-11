namespace sistema_gestao_recursos_humanos.backend.models
{
    public class PayHistory
    {
        public int PayHistoryId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime RateChangeDate { get; set; }
        public decimal Rate { get; set; }
        public int PayFrequency { get; set; }

        //public Employee Employee { get; set; }
    }
}