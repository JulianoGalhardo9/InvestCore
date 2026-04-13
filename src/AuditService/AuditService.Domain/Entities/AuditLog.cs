namespace AuditService.Domain.Entities;
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EventName { get; private set; }
    public string EventData { get; private set; }
    public DateTime Timestamp { get; private set; }

    public AuditLog(string eventName, string eventData)
    {
        Id = Guid.NewGuid();
        EventName = eventName;
        EventData = eventData;
        Timestamp = DateTime.UtcNow;
    }
}