using System.ComponentModel.DataAnnotations;

namespace HandleConcurrency.Data;

/// <summary>
/// Product table. Concurrency will be handled manually using the fields
/// VersionInfo and VersionQuantities.
/// </summary>
public class Product : AbstractBaseTable
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Concurrency Token for the 'Info' subset of fields (Name).
    /// </summary>
    public long VersionInfo { get; set; }

    public long ItemsInStock { get; set; }
    public long ItemsInOrder { get; set; }
    /// <summary>
    /// Concurrency Token for the 'Quantities' subset of fields
    /// (ItemsInStock, ItemsInOrder)
    /// </summary>
    public long VersionQuantities { get; set; }
}
