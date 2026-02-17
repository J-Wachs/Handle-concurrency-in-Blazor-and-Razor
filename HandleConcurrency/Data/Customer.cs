using System.ComponentModel.DataAnnotations;

namespace HandleConcurrency.Data;

/// <summary>
/// Customer entity with Id, maintenance fields and concurrency stamp
/// though inheritance. Optimistic locking.
/// </summary>
public class Customer : AbstractOptimisticConcurrencyTable
{
    [MaxLength(50), Required]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50), Required]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20), Required]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(50), Required]
    public string City { get; set; } = string.Empty;

    // In the database context, a unique index is build over this field. The repository abstraction
    // contains code to intercept dublicate key exceptions.
    [Required]
    [Range(1, 9999999999, ErrorMessage = "Bank account number must be at least 1 and max of 10 digits.")]
    public long BankAccount { get; set; } = 0;
}
