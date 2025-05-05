using Microsoft.Data.SqlClient;
using WebApplication1.Repositories.Abstract;
using WebApplication1.Entities;


public class ClientRepository : IClientRepository
{

    private readonly IConfiguration _configuration;

    public ClientRepository(IConfiguration configuration) => _configuration = configuration;

    public async Task<List<Trip>> GetTripsAsync(CancellationToken token = default)
    {
        const string query = """
                                 SELECT 
                                     T.IdTrip, T.Name, T.DateFrom, T.DateTo, 
                                     T.Description, T.MaxPeople,
                                     C.IdCountry, C.Name AS CountryName,
                                     CT.IdClient, CT.RegisteredAt, CT.PaymentDate
                                 FROM Trip T
                                 LEFT JOIN Country_Trip CT2 ON CT2.IdTrip = T.IdTrip
                                 LEFT JOIN Country C ON C.IdCountry = CT2.IdCountry
                                 LEFT JOIN Client_Trip CT ON CT.IdTrip = T.IdTrip
                                 ORDER BY T.IdTrip
                             """;

        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var cmd = new SqlCommand(query, con);
        await con.OpenAsync(token);

        await using var reader = await cmd.ExecuteReaderAsync(token);

        var tripDictionary = new Dictionary<int, Trip>();

        while (await reader.ReadAsync(token))
        {
            var tripId = reader.GetInt32(0);

            if (!tripDictionary.TryGetValue(tripId, out var trip))
            {
                trip = new Trip
                {
                    Id = tripId,
                    Name = reader.GetString(1),
                    DateFrom = reader.GetDateTime(2),
                    DateTo = reader.GetDateTime(3),
                    Description = reader.GetString(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = [],
                    Participants = []
                };
                tripDictionary[tripId] = trip;
            }

            if (!reader.IsDBNull(6))
            {
                var countryId = reader.GetInt32(6);
                var countryName = reader.GetString(7);
                if (trip.Countries.All(c => c.Id != countryId))
                {
                    trip.Countries.Add(new Country
                    {
                        Id = countryId,
                        Name = countryName
                    });
                }
            }

            if (!reader.IsDBNull(8))
            {
                var clientId = reader.GetInt32(8);
                var registeredAt = reader.GetInt32(9);
                var paymentDate = reader.GetInt32(10);

                if (trip.Participants.All(p => p.ClientId != clientId))
                {
                    trip.Participants.Add(new ClientTrip
                    {
                        ClientId = clientId,
                        TripId = tripId,
                        RegisteredAt = registeredAt,
                        PaymentDate = paymentDate
                    });
                }
            }
        }

        return tripDictionary.Values.ToList();
    }


    public async Task<bool> ClientExistsAsync(int clientId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        var cmd = new SqlCommand("SELECT IFF(EXISTS(SELECT 1 FROM CLIENT WHERE CLIENT.IdClient = @idClient),1, 0)",
            connection);
        cmd.Parameters.AddWithValue("@idClient", clientId);
        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));

        return result == 1;
    }

    public async Task<Client>? GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        var cmd = new SqlCommand("SELECT * FROM CLIENT WHERE IdClient = @clientId", connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new Client()
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Email = reader.GetString(3),
            Telephone = reader.GetString(4),
            Pesel = reader.GetString(5),
            Trips = null
        };
    }

    public async Task<List<ClientTrip>?> GetClientTripsAsync(int clientId, CancellationToken token = default)
    {
        const string query = """
                                 SELECT 
                                     CT.IdClient, CT.IdTrip, 
                                     CT.RegisteredAt, CT.PaymentDate
                                 FROM Client_Trip CT
                                 WHERE CT.IdClient = @clientId
                             """;

        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@clientId", clientId);

        await con.OpenAsync(token);
        await using var reader = await cmd.ExecuteReaderAsync(token);

        var trips = new List<ClientTrip>();

        while (await reader.ReadAsync(token))
        {
            var registeredAt = reader.GetDateTime(2);
            var paymentDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);

            trips.Add(new ClientTrip
            {
                ClientId = reader.GetInt32(0),
                TripId = reader.GetInt32(1),
                RegisteredAt = (int)((DateTimeOffset)registeredAt).ToUnixTimeSeconds(),
                PaymentDate = paymentDate.HasValue
                    ? (int)((DateTimeOffset)paymentDate.Value).ToUnixTimeSeconds()
                    : null
            });
        }

        return trips;
    }

    public async Task<Client> CreateClientAsync(Client client, CancellationToken token = default)
    {
        const string query = """
                             INSERT INTO Client(FirstName, LastName, Email, Telephone, Pesel)
                             VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)
                             SELECT SCOPE_IDENTITY();
                             """;

        await using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
        await using SqlCommand command = new SqlCommand(query, con);
        await con.OpenAsync(token);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        var result = await command.ExecuteScalarAsync(token);
        client.Id = Convert.ToInt32(result);
        return client;
    }
    public async Task<bool> AddClientToTrip(int clientId, int tripId, CancellationToken token = default)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);

        var checkClientCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @clientId", con);
        checkClientCmd.Parameters.AddWithValue("@clientId", clientId);
        var clientExists = await checkClientCmd.ExecuteScalarAsync(token) is not null;
        if (!clientExists) return false;

        var checkTripCmd = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @tripId", con);
        checkTripCmd.Parameters.AddWithValue("@tripId", tripId);
        var tripExists = await checkTripCmd.ExecuteScalarAsync(token) is not null;
        if (!tripExists) return false;

        var checkRegisteredCmd = new SqlCommand("""
                                                    SELECT 1 FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId
                                                """, con);
        checkRegisteredCmd.Parameters.AddWithValue("@clientId", clientId);
        checkRegisteredCmd.Parameters.AddWithValue("@tripId", tripId);
        var alreadyRegistered = await checkRegisteredCmd.ExecuteScalarAsync(token) is not null;
        if (alreadyRegistered) return false;

        var insertCmd = new SqlCommand("""
                                           INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                                           VALUES (@clientId, @tripId, @registeredAt)
                                       """, con);

        var registeredAt = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        insertCmd.Parameters.AddWithValue("@clientId", clientId);
        insertCmd.Parameters.AddWithValue("@tripId", tripId);
        insertCmd.Parameters.AddWithValue("@registeredAt", registeredAt);

        var result = await insertCmd.ExecuteNonQueryAsync(token);
        return result > 0;
    }

    public async Task<bool> RemoveClientFromTrip(int clientId, int tripId, CancellationToken token = default)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);

        var checkClientCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @clientId", con);
        checkClientCmd.Parameters.AddWithValue("@clientId", clientId);
        var clientExists = await checkClientCmd.ExecuteScalarAsync(token) is not null;
        if (!clientExists) return false;

        var checkTripCmd = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @tripId", con);
        checkTripCmd.Parameters.AddWithValue("@tripId", tripId);
        var tripExists = await checkTripCmd.ExecuteScalarAsync(token) is not null;
        if (!tripExists) return false;

        var checkRegisteredCmd = new SqlCommand("""
                                                    SELECT 1 FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId
                                                """, con);
        checkRegisteredCmd.Parameters.AddWithValue("@clientId", clientId);
        checkRegisteredCmd.Parameters.AddWithValue("@tripId", tripId);
        var alreadyRegistered = await checkRegisteredCmd.ExecuteScalarAsync(token) is null;
        if (alreadyRegistered) return false; 

        var deleteCmd = new SqlCommand("""
                                           DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId
                                       """, con);
        deleteCmd.Parameters.AddWithValue("@clientId", clientId);
        deleteCmd.Parameters.AddWithValue("@tripId", tripId);

        var result = await deleteCmd.ExecuteNonQueryAsync(token);
        return result > 0;
    }
}

