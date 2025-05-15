namespace PrzykladoweKolokwium.Models.DTOs;
using System;

public class CustomerDto
{
    public string FirstName { get; set; } 
    public string LastName { get; set; } 
    public List<RentalDto> Rentals { get; set; } 
}
public class RentalDto
{
    public int Id { get; set; } 
    public DateTime RentalDate { get; set; } 
    public DateTime? ReturnDate { get; set; } 
    public string Status { get; set; } = string.Empty;
    public List<MovieDto> Movies { get; set; }
}
public class MovieDto
{
    public string Title { get; set; }
    public decimal PriceAtRental { get; set; }
}

public class CreateRentalDto
{
    public int Id { get; set; }
    public DateTime RentalDate { get; set; }
    public List<MovieDto> Movies { get; set; }
}