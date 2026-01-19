using System.ComponentModel.DataAnnotations;

namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class PayHistoryDto
    {
        public int BusinessEntityID { get; set; }
        public DateTime RateChangeDate { get; set; }
        [Range(6, 199, ErrorMessage = "O valor deve ser maior que 5 e menor que 200.")]
 
        public decimal Rate { get; set; }
        public byte PayFrequency { get; set; }
    }
}