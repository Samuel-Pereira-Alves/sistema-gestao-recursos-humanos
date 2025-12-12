namespace sistema_gestao_recursos_humanos.backend.models.dtos
{

    public class ApproveCandidateDTO
    {
        public EmployeeDto Employee { get; set; } = default!;

        public string? Username { get; set; }
        public string? Role { get; set; } = "Employee";

        // Em produção, evita aceitar Password; aqui só para dev/seed
        public string? TempPassword { get; set; } // se não vier, é gerada
    }

}