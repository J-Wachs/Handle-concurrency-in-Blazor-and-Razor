using HandleConcurrency.Data;
using HandleConcurrency.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace HandleConcurrency.Repositories;

/// <summary>
/// Base abstraction for repositoty. Contains the basic Get* methods.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class AbstractBaseRepository<TEntity> where TEntity : AbstractBaseTable
{
    protected IDbContextFactory<DatabaseContext> DatabaseFactory { get; init; }

    protected ILogger<AbstractBaseRepository<TEntity>> Logger { get; init; }

    private string? _tableName = null;

    // Hold values that will be reset in case of error:
    private DateTime _saveCreated = default;
    private string _saveCreatedBy = string.Empty;
    private DateTime? _saveModified = default;
    private string? _saveModifiedBy = string.Empty;

    /// <summary>
    /// You will get a 'Use primary constructor' warning, but this constructor is
    /// by design in order to avoid parameter capture warnings in derived classes.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="databaseFactory"></param>
    public AbstractBaseRepository(
        ILogger<AbstractBaseRepository<TEntity>> logger,
        IDbContextFactory<DatabaseContext> databaseFactory)
    {
        Logger = logger;
        DatabaseFactory = databaseFactory;
    }

    /// <summary>
    /// Retrieves all records that meet the filter criteria and the PageNumber/PageSize.
    /// </summary>
    /// <param name="filter">Filter expression to use when retrieving the data</param>
    /// <param name="sorting">The descriptors of the sorting</param>
    /// <param name="pageNumber">Pagenumber to retrive</param>
    /// <param name="pageSize">Number of records to retrieve</param>
    /// <returns>Result object</returns>
    public async Task<Result<List<TEntity>?>> GetAsync(Expression<Func<TEntity, bool>>? filter, IEnumerable<SortDescriptor> sorting, int pageNumber, int pageSize)
    {
        string methodName = $"{nameof(GetAsync)}";

        using var databaseContext = DatabaseFactory.CreateDbContext();

        try
        {
            pageNumber = (pageNumber < RepositoryConstants.FirstPage ? RepositoryConstants.FirstPage : pageNumber);
            if (pageSize > RepositoryConstants.MaxRowsPerPage)
            {
                pageSize = RepositoryConstants.MaxRowsPerPage;
            }
            else if (pageSize <= RepositoryConstants.Zero)
            {
                pageSize = RepositoryConstants.DefaultRowsPerPage;
            }

            var query = databaseContext.Set<TEntity>()
                .AsNoTracking()
                .AsQueryable();

            // Apply filter:
            if (filter is not null)
            {
                query = query.Where(filter);
            }

            // Apply sorting by calling extension method:
            query = query.ApplySorting(sorting);
            
            // Apply offset and number of records to read:
            query = query.Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize);

            var listOfRecords = await query.ToListAsync();


            listOfRecords ??= [];

            return Result<List<TEntity>?>.Success(listOfRecords);
        }
        catch (SqlException ex) when (ex.Number is (int)SqlErrors.Timeout)
        {
            string tableName = GetTableName(databaseContext);
            if (databaseContext is not null)
            {
                await databaseContext.Database.RollbackTransactionAsync();
            }
            return Result<List<TEntity>?>.FailureConflict("The record you want to update is currently in use. Try again later.");
        }
        catch (Exception ex)
        {
            string paramList = $"(filter, sorting, {pageNumber}, {pageSize})";
            Logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<List<TEntity>?>.Fatal($"Error occured in '{methodName}'. The error is: '{ex.Message}'.");
        }
    }

    /// <summary>
    /// Retrieves the record with the supplied Id.
    /// </summary>
    /// <param name="id">The id of the record to retrieve</param>
    /// <returns>Result object</returns>
    public async Task<Result<TEntity?>> GetByIdAsync(long id)
    {
        string methodName = $"{nameof(GetByIdAsync)}";

        using var databaseContext = DatabaseFactory.CreateDbContext();

        try
        {
            var record = await databaseContext.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (record is null)
            {
                return Result<TEntity?>.FailureNotFound($"Error occured in '{methodName}'. The error is: 'There is no record with key {id}'.");
            }
            return Result<TEntity?>.Success(record);
        }
        catch (Exception ex)
        {
            var paramList = $"({id})";
            Logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal($"Error occured in '{methodName}'. The error is: '{ex.Message}'.");
        }
    }

    /// <summary>
    /// Retrieves the actual table name using in db, based on the TEntity type.
    /// </summary>
    /// <param name="databaseContext">The database context used to connect to database/table</param>
    /// <returns>Name of the table in question</returns>
    /// <exception cref="Exception">If the database context does not contain the TEntity type</exception>
    protected string GetTableName(DatabaseContext databaseContext)
    {
        if (_tableName is null)
        {
            var entityType = databaseContext.Model.FindEntityType(typeof(TEntity));
            if (entityType is null)
            {
                Logger.LogCritical("Error occured in 'GetTableName'. The error is: 'Could not obtain the entity type'.");
                throw new Exception("Error occured in 'GetTableName'. The error is: 'Could not obtain the entity type'.");
            }
            _tableName = entityType.GetTableName();
            if (_tableName is null)
            {
                Logger.LogCritical("Error occured in 'GetTableName'. The error is: 'Could not obtain the table name'.");
                throw new Exception("Error occured in 'GetTableName'. The error is: 'Could not obtain the table name'.");
            }
        }

        return _tableName;
    }

    /// <summary>
    /// Stores the fields will get overwritten by Add and Update,
    /// in case we need to 'restore' them.
    /// </summary>
    /// <param name="entity"></param>
    protected void StoreSystemValues(TEntity entity)
    {
        _saveCreated = entity.Created;
        _saveCreatedBy = entity.CreatedBy;
        _saveModified = entity.Modified;
        _saveModifiedBy = entity.ModifiedBy;
    }


    /// <summary>
    /// Restores the fields that has been overwritten by Add and Update.
    /// </summary>
    /// <param name="entity"></param>
    protected void RestoreSystemValues(TEntity entity)
    {
        entity.Created = _saveCreated;
        entity.CreatedBy = _saveCreatedBy;
        entity.Modified = _saveModified;
        entity.ModifiedBy = _saveModifiedBy;
    }
}
