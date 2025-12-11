namespace sistema_gestao_recursos_humanos.backend.models
{
    public class HRUser
    {
        public int HRUserId { get; set; }
        public int EmployeeId { get; set; }
        public string Role { get; set; } = "HR";

        public bool CanEditProfiles { get; set; } = true;
        public bool CanManagePayments { get; set; } = true;
        public bool CanAccessResumes { get; set; } = true;
        public bool CanApproveCandidates { get; set; } = true;

        public Employee? Employee { get; set; }
    }
}