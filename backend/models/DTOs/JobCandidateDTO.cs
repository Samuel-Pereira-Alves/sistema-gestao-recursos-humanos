namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class JobCandidateDto
    {
        public int JobCandidateId { get; set; }
        public string Resume { get; set; } = string.Empty;

        public string CvFileUrl { get; set; } = "";
        public DateTime ModifiedDate { get; set; }

        public DateTime BirthDate { get; set; }
        public string NationalIDNumber { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}