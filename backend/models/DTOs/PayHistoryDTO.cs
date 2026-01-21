using System.ComponentModel.DataAnnotations;

namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class PayHistoryDto
    {
        public int BusinessEntityID { get; set; }
        public DateTime RateChangeDate { get; set; }
        //[Range(6.5, 200, ErrorMessage = "O valor deve estar entre 6.5 e 200.")]
 
        public decimal Rate { get; set; }
        public Person? Person {get; set;}
        public byte PayFrequency { get; set; }
    }
}