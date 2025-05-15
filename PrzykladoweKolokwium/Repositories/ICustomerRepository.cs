using PrzykladoweKolokwium.Models.DTOs;

namespace PrzykladoweKolokwium.Repositories;

public interface ICustomerRepository
{
    
    Task<CustomerDto?> GetCustomerWithRentalsAsync(int customerId);
    Task<bool> AddRentalAsync(int customerId, RentalDto rental);
}