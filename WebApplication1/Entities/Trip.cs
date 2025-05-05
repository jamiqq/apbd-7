namespace WebApplication1.Entities;

public class Trip : BaseEntity
{
    public string Name { get; set; } = String.Empty;
    
    public string Description { get; set; } = String.Empty;
    
    public DateTime DateFrom { get; set; }
    
    public DateTime DateTo { get; set; }
    
    public int MaxPeople { get; set; }

    public List<Country> Countries { get; set; } = [];

    public List<ClientTrip> Participants { get; set; } = [];
}