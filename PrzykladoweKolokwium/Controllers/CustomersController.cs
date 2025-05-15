using Microsoft.AspNetCore.Mvc;
using PrzykladoweKolokwium.Models.DTOs;
using PrzykladoweKolokwium.Repositories;

namespace PrzykladoweKolokwium.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController :ControllerBase
{
    private readonly ICustomerRepository _repo;

    public CustomersController(ICustomerRepository repo)
    {
        _repo = repo;
    }
    
    [HttpGet("{id}/rentals")]
    public async Task<IActionResult> GetCustomerRentals(int id)
    {
        var customer = await _repo.GetCustomerWithRentalsAsync(id);
        if (customer == null)
            return NotFound();
        
        return Ok(customer);
    }
    [HttpPost("{id}/rentals")]
    public async Task<IActionResult> AddRental(int id, [FromBody] RentalDto rental)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _repo.AddRentalAsync(id, rental);

        if (!result)
            return NotFound($"Customer with ID {id} not found.");

        return CreatedAtAction(nameof(GetCustomerRentals), new { id = id }, rental);
    }

    }
