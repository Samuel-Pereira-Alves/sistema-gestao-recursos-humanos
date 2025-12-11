namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class PayHistoryDto
    {
        public DateTime RateChangeDate { get; set; }
        public decimal Rate { get; set; }
        public int PayFrequency { get; set; }
    }
}