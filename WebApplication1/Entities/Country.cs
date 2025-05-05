namespace WebApplication1.Entities;

public class Country : BaseEntity
{
    public string Name { get; set; } = String.Empty;
    public List<Trip> Trips { get; set; } = [];
}