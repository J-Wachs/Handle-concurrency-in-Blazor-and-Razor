namespace HandleConcurrency.Data.DTOs;

/// <summary>
/// Subset of fields from Product to be edited sperately.
/// </summary>
public class ProductQtyDTO : AbstractBaseTable
{
    public long ItemsInOrder { get; set; }
    public long ItemsInStock { get; set; }
    public long VersionQuantities { get; set; }

    /// <summary>
    /// Mapping of Product into ProductQtyDTO.
    /// </summary>
    /// <param name="product">The product to be mapped</param>
    public static implicit operator ProductQtyDTO(Product product)
    {
        ProductQtyDTO productInfo = new()
        {
            Id = product.Id,
            ItemsInOrder = product.ItemsInOrder,
            ItemsInStock = product.ItemsInStock,
            VersionQuantities = product.VersionQuantities,
            Created = product.Created,
            CreatedBy = product.CreatedBy,
            Modified = product.Modified,
            ModifiedBy = product.ModifiedBy
        };

        return productInfo;
    }
}
