namespace OrderService.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedDate { get; private set; }
    public string? Error { get; private set; }

    public OutboxMessage(string type, string content)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOn = DateTime.UtcNow;
    }
    public void MarkAsProcessed()
    {
        ProcessedDate = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }
}