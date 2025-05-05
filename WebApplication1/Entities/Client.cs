namespace WebApplication1.Entities;

public class Client : BaseEntity
{
    public string FirstName { get; set; } = String.Empty;
    
    public string LastName { get; set; } = String.Empty;
    
    public string Email { get; set; } = String.Empty;
    
    public string Telephone { get; set; } = String.Empty;
    
    public string Pesel { get; set; } = String.Empty;
    
    public List<ClientTrip>? Trips { get; set; }
}