namespace RaceDayApi.Models;

public class EventType
{
    public int EventTypeId { get; set; }

    // "Run", "Walk", or "Cycle"
    public string TypeName { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
