using HandleConcurrency.Data;

namespace HandleConcurrencyBlazorDemo.Controllers.DTOs;

/// <summary>
/// DTO for returning data to caller.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Address"></param>
/// <param name="ZipCode"></param>
/// <param name="City"></param>
/// <param name="BankAccount"></param>
/// <param name="Version"></param>
/// <param name="Created"></param>
/// <param name="CreatedBy"></param>
/// <param name="Modified"></param>
/// <param name="ModifiedBy"></param>
public record CustomerDTO(
    long Id,
    string Name,
    string Address,
    string ZipCode,
    string City,
    long BankAccount,
    byte[] Version,
    DateTime Created,
    string CreatedBy,
    DateTime? Modified,
    string? ModifiedBy
    )
{
    /// <summary>
    /// Convert from entity object to DTO.
    /// </summary>
    /// <param name="customer"></param>
    public static implicit operator CustomerDTO(Customer customer)
    {
        return new CustomerDTO(
            Id: customer.Id,
            Name: customer.Name,
            Address: customer.Address,
            ZipCode: customer.ZipCode,
            City: customer.City,
            BankAccount: customer.BankAccount,
            Version: customer.Version,
            Created: customer.Created,
            CreatedBy: customer.CreatedBy,
            Modified: customer.Modified,
            ModifiedBy: customer.ModifiedBy
        );
    }
};
