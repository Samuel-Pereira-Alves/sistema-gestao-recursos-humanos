
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("JobCandidate", Schema = "HumanResources")]
    public class JobCandidate
    {
        public int JobCandidateId { get; set; }
        public int? BusinessEntityId { get; set; }
        public string? Resume { get; set; }

        public string? CvFileUrl { get; set; }
        
        public DateTime ModifiedDate { get; set; }
    }
}