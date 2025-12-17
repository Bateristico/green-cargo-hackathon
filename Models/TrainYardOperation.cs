namespace CouchbaseHackathonApp.Models;

public class TrainYardOperation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WagonId { get; set; } = string.Empty;
    public string WagonNumber { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
