using PrzykladoweKolokwium.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace PrzykladoweKolokwium.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IConfiguration _config;

    public CustomerRepository(IConfiguration config)
    {
        _config = config;
    }

    public async Task<CustomerDto?> GetCustomerWithRentalsAsync(int customerId)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT first_name, last_name, r.rental_id, rental_date, return_date, s.name, ri.price_at_rental, m.title
            FROM Rental r
            JOIN Customer c ON r.customer_id = c.customer_id
            JOIN Status s ON r.status_id = s.status_id
            JOIN Rental_Item ri ON ri.rental_id = r.rental_id
            JOIN Movie m ON m.movie_id = ri.movie_id
            WHERE r.customer_id = @customerId;", conn);

        cmd.Parameters.AddWithValue("@customerId", customerId);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        var customer = new CustomerDto
        {
            FirstName = reader.GetString(0),
            LastName = reader.GetString(1),
            Rentals = new List<RentalDto>()
        };

        var rentals = new Dictionary<int, RentalDto>();

        do
        {
            int rentalId = reader.GetInt32(2);

            if (!rentals.TryGetValue(rentalId, out var rental))
            {
                rental = new RentalDto
                {
                    Id = rentalId,
                    RentalDate = reader.GetDateTime(3),
                    ReturnDate = await reader.IsDBNullAsync(4) ? (DateTime?)null : reader.GetDateTime(4),
                    Status = reader.GetString(5),
                    Movies = new List<MovieDto>()
                };

                rentals[rentalId] = rental;
                customer.Rentals.Add(rental);
            }

            rental.Movies.Add(new MovieDto
            {
                Title = reader.GetString(7),
                PriceAtRental = reader.GetDecimal(6)
            });

        } while (await reader.ReadAsync());

        return customer;
    }

    public async Task<bool> AddRentalAsync(int customerId, RentalDto rental)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        using var tran = conn.BeginTransaction();

        try
        {
            // 1️⃣ Sprawdzenie czy klient istnieje
            var checkCustomerCmd = new SqlCommand(
                "SELECT COUNT(1) FROM Customer WHERE customer_id = @customerId", conn, tran);
            checkCustomerCmd.Parameters.AddWithValue("@customerId", customerId);

            if ((int)await checkCustomerCmd.ExecuteScalarAsync() == 0)
                return false;

            // 2️⃣ Dodanie wypożyczenia do tabeli Rental
            var insertRentalCmd = new SqlCommand(@"
                INSERT INTO Rental (rental_id, customer_id, rental_date, status_id) 
                VALUES (@Id, @CustomerId, @RentalDate, @StatusId)", conn, tran);

            insertRentalCmd.Parameters.AddWithValue("@Id", rental.Id);
            insertRentalCmd.Parameters.AddWithValue("@CustomerId", customerId);
            insertRentalCmd.Parameters.AddWithValue("@RentalDate", rental.RentalDate);

            // Ustal status ID - możesz dynamicznie pobierać z tabeli Status lub przypisać na sztywno.
            // Zakładam, że status 'Rented' ma ID = 1.
            insertRentalCmd.Parameters.AddWithValue("@StatusId", 1);

            await insertRentalCmd.ExecuteNonQueryAsync();

            // 3️⃣ Dodanie powiązanych filmów do Rental_Item
            foreach (var movie in rental.Movies)
            {
                var getMovieIdCmd = new SqlCommand(
                    "SELECT movie_id FROM Movie WHERE title = @Title", conn, tran);
                getMovieIdCmd.Parameters.AddWithValue("@Title", movie.Title);

                var movieIdObj = await getMovieIdCmd.ExecuteScalarAsync();
                if (movieIdObj == null)
                    throw new Exception($"Movie '{movie.Title}' does not exist.");

                int movieId = (int)movieIdObj;

                var insertRentalItemCmd = new SqlCommand(@"
                    INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental) 
                    VALUES (@RentalId, @MovieId, @Price)", conn, tran);

                insertRentalItemCmd.Parameters.AddWithValue("@RentalId", rental.Id);
                insertRentalItemCmd.Parameters.AddWithValue("@MovieId", movieId);
                insertRentalItemCmd.Parameters.AddWithValue("@Price", movie.PriceAtRental);

                await insertRentalItemCmd.ExecuteNonQueryAsync();
            }

            // 4️⃣ Zatwierdzenie transakcji
            await tran.CommitAsync();
            return true;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
}
