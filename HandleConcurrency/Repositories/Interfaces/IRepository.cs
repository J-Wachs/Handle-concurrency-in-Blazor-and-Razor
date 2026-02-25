using HandleConcurrency.Data;
using System.Linq.Expressions;

namespace HandleConcurrency.Repositories.Interfaces;

/// <summary>
/// Interface for repository that manually handles concurrency check.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IRepository<TEntity> where TEntity : AbstractBaseTable
{
    /// <summary>
    /// Adds a record to the entity table.
    /// </summary>
    /// <param name="record">The record to be added</param>
    /// <param name="userId">The user Id to be stamped as the creator of the record</param>
    /// <returns>Result object with record containing the assigned Id</returns>
    Task<Result<TEntity?>> AddAsync(TEntity record, string userId);

    /// <summary>
    /// Deletes a record from the entity table.
    /// </summary>
    /// <param name="record">The record to be deleted</param>
    /// <returns>Result object</returns>
    Task<Result> DeleteAsync(TEntity record);

    /// <summary>
    /// Retrieves all records that meet the filter criteria and the PageNumber/PageSize.
    /// </summary>
    /// <param name="filter">Filter expression to use when retrieving the data</param>
    /// <param name="sorting">The descriptors of the sorting</param>
    /// <param name="pageNumber">Pagenumber to retrive</param>
    /// <param name="pageSize">Number of records to retrieve</param>
    /// <returns>Result object</returns>
    Task<Result<List<TEntity>?>> GetAsync(Expression<Func<TEntity, bool>> filter, IEnumerable<SortDescriptor> sorting, int pageNumber, int pageSize);

    /// <summary>
    /// Retrieves the record with the supplied Id.
    /// </summary>
    /// <param name="id">The id of the record to retrieve</param>
    /// <returns>Result object</returns>
    Task<Result<TEntity?>> GetByIdAsync(long id);

    /// <summary>
    /// Updates the record in the entity table.
    /// </summary>
    /// <param name="record">Updated records to store</param>
    /// <param name="userId">The user Id to be stamped as the modifying user of the record</param>
    /// <returns>Result object</returns>
    Task<Result<TEntity?>> UpdateAsync(TEntity record, string userId);
}
