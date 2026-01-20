using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class NotificationDto
    {
        public int ID { get; set; }
        public string Message { get; set; } = string.Empty;
        public int BusinessEntityID { get; set; }
        
        public DateTime CreatedAt  {get; set;}

        public string? Type {get; set;}
    }
}
