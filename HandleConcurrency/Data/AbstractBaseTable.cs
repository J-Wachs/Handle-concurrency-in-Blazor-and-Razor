using System.ComponentModel.DataAnnotations;

namespace HandleConcurrency.Data;

/// <summary>
/// Abstractiíon containing standard fields for tables.
/// </summary>
public abstract class AbstractBaseTable
{
    [Key]
    public long Id { get; set; }

    public DateTime Created { get; set; }

    [MaxLength(40)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? Modified { get; set; }

    [MaxLength(40)]
    public string? ModifiedBy { get; set; }
}
