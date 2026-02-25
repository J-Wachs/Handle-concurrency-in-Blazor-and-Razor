namespace HandleConcurrency.Data.DTOs;

/// <summary>
/// Subset of fields from Product to be edited sperately.
/// </summary>
public class ProductInfoDTO : AbstractBaseTable
{
    public string Name { get; set; } = string.Empty;
    public long VersionInfo { get; set; }

    /// <summary>
    /// Mapping of Product into ProductInfoDTO.
    /// </summary>
    /// <param name="product">The product to be mapped</param>
    public static implicit operator ProductInfoDTO(Product product)
    {
        ProductInfoDTO productInfo = new()
        {
            Id = product.Id,
            Name = product.Name,
            VersionInfo = product.VersionInfo,
            Created = product.Created,
            CreatedBy = product.CreatedBy,
            Modified = product.Modified,
            ModifiedBy = product.ModifiedBy
        };

        return productInfo;
    }
}
