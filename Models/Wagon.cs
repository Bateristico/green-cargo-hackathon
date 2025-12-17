namespace CouchbaseHackathonApp.Models;

public class Wagon
{
    public string Id { get; set; } = string.Empty;
    public string WagonNumber { get; set; } = string.Empty;
    public string WagonType { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public string CurrentLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool RequiresLegalCheck { get; set; }
    public DateTime LastInspection { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
