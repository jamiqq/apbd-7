using WebApplication1.Entities;

namespace WebApplication1.Repositories.Abstract;

public interface IClientRepository
{
    public Task<Client>? GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default);
    
    public Task<List<ClientTrip>?> GetClientTripsAsync(int clientId, CancellationToken token = default);
    
    public Task<Client> CreateClientAsync(Client client, CancellationToken token = default);
    
    public  Task<List<Trip>>? GetTripsAsync(CancellationToken token = default);
    
    public Task<bool> AddClientToTrip(int clientId, int tripId, CancellationToken token = default);
    
    public Task<bool> RemoveClientFromTrip(int clientId, int tripId, CancellationToken token = default);
}