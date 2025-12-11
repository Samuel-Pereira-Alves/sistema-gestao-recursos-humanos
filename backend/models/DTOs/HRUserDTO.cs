namespace sistema_gestao_recursos_humanos.backend.models.dtos
{
    public class HRUserDto
    {
        public int HRUserId { get; set; }
        public int EmployeeId { get; set; }
        public string Role { get; set; } = "HR";

        public bool CanEditProfiles { get; set; }
        public bool CanManagePayments { get; set; }
        public bool CanAccessResumes { get; set; }
        public bool CanApproveCandidates { get; set; }

        public EmployeeDto Employee { get; set; } = new();
    }
}