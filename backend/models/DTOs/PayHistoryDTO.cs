namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class PayHistoryDto
    {
        public int BusinessEntityID { get; set; }
        public DateTime RateChangeDate { get; set; }
        public decimal Rate { get; set; }
        public byte PayFrequency { get; set; }
    }
}