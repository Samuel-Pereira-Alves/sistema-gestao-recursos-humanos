
using System.ComponentModel.DataAnnotations.Schema;

namespace sistema_gestao_recursos_humanos.backend.models
{
    [Table("JobCandidate", Schema = "HumanResources")]
    public class JobCandidate
    {
        public int JobCandidateId { get; set; }
        public int? BusinessEntityId { get; set; }
        public string? Resume { get; set; }

        public byte[]? CvFileBytes { get; set; }
        public string? Email {get; set;}
        
        public DateTime ModifiedDate { get; set; }

        public DateTime BirthDate { get; set; }
        public string NationalIDNumber { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}