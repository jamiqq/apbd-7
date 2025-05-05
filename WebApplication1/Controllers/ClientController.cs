using WebApplication1.Entities;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/clients")]
    public class ClientController : ControllerBase
    {
        private readonly ClientRepository _clientRepository;

        public ClientController(ClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetAllTrips(CancellationToken token)
        {
            var trips = await _clientRepository.GetTripsAsync(token);
            if (trips == null || !trips.Any())
                return NotFound("No trips found.");
            
            return Ok(trips);
        }

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id, CancellationToken token)
        {
            var trips = await _clientRepository.GetClientTripsAsync(id, token);
            if (trips == null || !trips.Any())
                return NotFound($"No trips found for client with ID {id}.");
            
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> AddClient([FromBody] Client client, CancellationToken token)
        {
            
            var result = await _clientRepository.CreateClientAsync(client, token);
            if (result == null)
                return BadRequest("Failed to create client.");
            
            return CreatedAtAction(nameof(GetClientTrips), new { id = client.Id }, client);
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> UpdateClientTrip(int id, int tripId, CancellationToken token)
        {
            var result = await _clientRepository.AddClientToTrip(id, tripId, token);
            if (!result)
                return NotFound($"Client-trip relationship not found for client {id} and trip {tripId}.");
            
            return NoContent();
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientTrip(int id, int tripId, CancellationToken token)
        {
            var result = await _clientRepository.RemoveClientFromTrip(id, tripId, token);
            if (!result)
                return NotFound($"Client-trip relationship not found for client {id} and trip {tripId}.");
            
            return NoContent();
        }
    }
}
