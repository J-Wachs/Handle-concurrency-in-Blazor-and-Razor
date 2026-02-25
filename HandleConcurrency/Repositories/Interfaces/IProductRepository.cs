using HandleConcurrency.Data;
using HandleConcurrency.Data.DTOs;

namespace HandleConcurrency.Repositories.Interfaces;

/// <summary>
/// Interface for a Product specific repository. This is due to the fact, that the sections of information
/// to be edited, must have their own update methods in order to manage the Version* fields.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Updates the field in the record in the entity table, that match fields in 'ProductInfoDTO'..
    /// </summary>
    /// <param name="record">Updated records to store</param>
    /// <param name="userId">The user Id to be stamped as the modifying user of the record</param>
    /// <returns>Result object</returns>
    Task<Result<ProductInfoDTO?>> UpdateInfoAsync(ProductInfoDTO record, string userId);

    /// <summary>
    /// Updates the fields in the record in the entity table, that match fields in 'ProductQtyDTO'.
    /// </summary>
    /// <param name="record">Updated records to store</param>
    /// <param name="userId">The user Id to be stamped as the modifying user of the record</param>
    /// <returns>Result object</returns>
    Task<Result<ProductQtyDTO?>> UpdateQuantitiesAsync(ProductQtyDTO record, string userId);
}
