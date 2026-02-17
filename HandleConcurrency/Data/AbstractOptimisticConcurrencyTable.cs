using System.ComponentModel.DataAnnotations;

namespace HandleConcurrency.Data;

/// <summary>
/// Abstraction for the MS SQL Server concurrency stamp
/// </summary>
public abstract class AbstractOptimisticConcurrencyTable : AbstractBaseTable
{
    /// <summary>
    /// MS SQL Server recognice this as the Concurrency stamp, and EF Core will
    /// automatically handle update of the field, and check against it upon 
    /// updates and deletes.
    /// </summary>
    [Timestamp]
    public byte[] Version { get; set; } = [];
}
