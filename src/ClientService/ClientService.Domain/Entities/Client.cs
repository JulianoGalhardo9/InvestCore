namespace ClientService.Domain;
public class Client
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SuitabilityProfile Profile { get; private set; }
    public Client(Guid userId, SuitabilityProfile profile)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Profile = profile;
    }
    public void UpdateProfile(SuitabilityProfile newProfile)
    {
        Profile = newProfile;
    }
}