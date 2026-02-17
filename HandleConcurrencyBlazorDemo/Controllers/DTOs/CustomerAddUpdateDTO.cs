using HandleConcurrency.Data;
using System.ComponentModel.DataAnnotations;

namespace HandleConcurrencyBlazorDemo.Controllers.DTOs;

/// <summary>
/// DTO for adding and updating customers.
/// </summary>
/// <param name="Name"></param>
/// <param name="Address"></param>
/// <param name="ZipCode"></param>
/// <param name="City"></param>
/// <param name="BankAccount"></param>
public record CustomerAddUpdateDTO(
    [MaxLength(50), Required]
    string Name,

    [MaxLength(50), Required]
    string Address,

    [MaxLength(20), Required]
    string ZipCode,

    [MaxLength(50), Required]
    string City,

    [Required]
    [Range(1, 9999999999, ErrorMessage = "Bank account number must be at least 1 and max of 10 digits.")]
    long BankAccount

    )
{
    /// <summary>
    /// Convert from DTO to entity object.
    /// </summary>
    /// <param name="customerDTO"></param>
    public static implicit operator Customer(CustomerAddUpdateDTO customerDTO)
    {
        return new Customer()
        {
            Name = customerDTO.Name,
            Address = customerDTO.Address,
            ZipCode = customerDTO.ZipCode,
            City = customerDTO.City,
            BankAccount = customerDTO.BankAccount
        };
    }

    /// <summary>
    /// Merges the DTO to the entity object.
    /// </summary>
    /// <param name="customer"></param>
    public void MergeTo(Customer customer)
    {
        customer.Name = Name;
        customer.Address = Address;
        customer.ZipCode = ZipCode;
        customer.City = City;
        customer.BankAccount = BankAccount;
    }
}
