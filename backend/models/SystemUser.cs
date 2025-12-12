public class SystemUser {
    public int SystemUserId { get; set; }
    public int BusinessEntityID { get; set; }
    public string? Username  { get; set; }
    public string? PasswordHash { get; set; }
    public string? Role { get; set; }
}