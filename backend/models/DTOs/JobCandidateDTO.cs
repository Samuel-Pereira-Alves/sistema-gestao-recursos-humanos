namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class JobCandidateDto
    {
        public int JobCandidateId { get; set; }
        public string Resume { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
    }
}