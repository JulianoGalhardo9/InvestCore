namespace IdentityService.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsKycApproved { get; private set; }
    public User(string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        IsKycApproved = false;
    }
    public void ApproveKyc()
    {
        IsKycApproved = true;
    }
}