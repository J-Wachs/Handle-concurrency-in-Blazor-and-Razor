using HandleConcurrency.Data;
using HandleConcurrency.Helpers;
using HandleConcurrency.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandleConcurrency.Repositories;

/// <summary>
/// Base abstraction for repository where the concurrency handling is done automatically in a combination of EF Core and this class.
/// The concurrency method used is optimistic.
/// This is the simplest form (code wise) of concurrency handling.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <param name="logger"></param>
/// <param name="databaseFactory"></param>
public abstract class AbstractOptimisticConcurrencyRepository<TEntity>(
    ILogger<AbstractOptimisticConcurrencyRepository<TEntity>> logger,
    IDbContextFactory<DatabaseContext> databaseFactory) : AbstractBaseRepository<TEntity>(logger, databaseFactory), IOptimisticConcurrencyRepository<TEntity>
        where TEntity : AbstractOptimisticConcurrencyTable
{
    /// <inheritdoc/>
    public async Task<Result<TEntity?>> AddAsync(TEntity record, string userId)
    {
        string methodName = $"{nameof(AddAsync)}", paramList = "(record, userId)";

        using var databaseContext = base.DatabaseFactory.CreateDbContext();

        try
        {
            StoreSystemValues(record);

            record.Id = 0;
            
            record.Created = DateTime.Now;
            record.CreatedBy = userId;

            // Microsoft SQL Server automatically sets the concurrency stamp in the 'version' field.
            _ = await databaseContext.Set<TEntity>()
                .AddAsync(record);
            await databaseContext.SaveChangesAsync();
            return Result<TEntity?>.SuccessCreated(record);
        }
        // If this is a doublicate key violation, inform the user
        catch (DbUpdateException ex) when (ex.InnerException is not null &&
                                            ex.GetBaseException() is SqlException && 
                                            ((SqlException) ex.InnerException).Number is (int)SqlErrors.ViolationUniqueKeyConstraint)
        {
            RestoreSystemValues(record);

            var tryGetDetailsResult = ex.TryGetUniqueConstraintViolationDetails();
            if (tryGetDetailsResult.IsSuccess is false)
            {
                return Result<TEntity?>.FailureConflict("A field that must contain a unique value in the table, contains a value that is already used.");
            }

            var errorMessage = $"The field '{tryGetDetailsResult.Data!.FieldName}' must have a unique value in the table '{tryGetDetailsResult.Data!.TableName}'. The value '{tryGetDetailsResult.Data!.KeyValue}' you are trying to save, is already used in a record in the table.";

            return Result<TEntity?>.FailureConflict(errorMessage);
        }
        catch (Exception ex)
        {
            RestoreSystemValues(record);

            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal("Could not add record to the database.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(TEntity record)
    {
        string methodName = $"{nameof(DeleteAsync)}", paramList = $"(record[Id={record.Id}])";

        using var databaseContext = base.DatabaseFactory.CreateDbContext();

        DateTime? modified = null;
        string? modifiedBy = null;

        try
        {
            // Microsoft SQL Server automatically ensures that the concurrency stamp in the record in input 
            // matches the one in the database. If they do not match, the record is not deleted.
            var dbRecord = await databaseContext.Set<TEntity>().FindAsync(record.Id);
            if (dbRecord is not null)
            {
                modified = dbRecord.Modified;
                modifiedBy = dbRecord.ModifiedBy;

                // Set the Concurrency token in the record:
                databaseContext.Entry(dbRecord).Property("Version").OriginalValue = record.Version;

                databaseContext.Set<TEntity>().Remove(dbRecord);
                await databaseContext.SaveChangesAsync();
            }

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            RestoreSystemValues(record);
            if (modified is null || modifiedBy is null)
            {
                return Result.FailureConflict($"The data could not be deleted because another user has changed the data after you read the data.");
            }

            return Result.FailureConflict($"The data could not be deleted because the user '{modifiedBy}' has changed the data on the '{modified}' which is after you read the data.");
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result.Fatal("The record could not be deleted.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<TEntity?>> UpdateAsync(TEntity record, string userId)
    {
        string methodName = $"{nameof(UpdateAsync)}";

        using var databaseContext = base.DatabaseFactory.CreateDbContext();

        DateTime? modified = null;
        string? modifiedBy = null;

        try
        {
            StoreSystemValues(record);

            record.Modified = DateTime.Now;
            record.ModifiedBy = userId;

            // EF Core automatically ensures that the concurrency stamp in the record in input 
            // matches the one in the database. If they do not match, the record is not updated
            // and an exception is thrown.
            var dbRecord = await databaseContext.Set<TEntity>()
                .FindAsync(record.Id);

            if (dbRecord is not null)
            {
                modified = dbRecord.Modified;
                modifiedBy = dbRecord.ModifiedBy;

                // Set the Concurrency token in the record:
                databaseContext.Entry(dbRecord).Property("Version").OriginalValue = record.Version;

                databaseContext.Entry(dbRecord).CurrentValues.SetValues(record);
                await databaseContext.SaveChangesAsync();
            }
            else
            {
                logger.LogError("An error occurred in '{methodName}'. The error is: 'There is no data in the table with the primary key provided '{record.Id}''.",
                    methodName, record.Id);
                return Result<TEntity?>.FailureNotFound($"The record with id {record.Id} could not be found in the database and therefore not updated.");
            }

            // Why returning the 'dbRecord' and not the 'record' that is in input?
            // Because the 'dbRecord' is the one that is updated by EF Core and therefore
            // has the concurrency stamp updated by EF Core after saving changes. The 'record'
            // in input does not have the concurrency stamp updated and therefore does not reflect
            // the actual state of the record in the database after saving changes.
            return Result<TEntity?>.Success(dbRecord);
        }
        catch (DbUpdateConcurrencyException)
        {
            RestoreSystemValues(record);
            if (modified is null || modifiedBy is null)
            {
                return Result<TEntity?>.FailureConflict($"The data could not be stored because another user has stored changed data after you read the data.");
            }

            return Result<TEntity?>.FailureConflict($"The data could not be stored because the user '{modifiedBy}' has stored changed data on the '{modified}' which is after you read the data.");
        }
        // If this is a doublicate key violation, inform the user
        catch (DbUpdateException ex) when (ex.InnerException is not null &&
                                            ex.GetBaseException() is SqlException &&
                                            ((SqlException)ex.InnerException).Number is (int)SqlErrors.ViolationUniqueKeyConstraint)
        {
            RestoreSystemValues(record);

            var tryGetDetailsResult = ex.TryGetUniqueConstraintViolationDetails();
            if (tryGetDetailsResult.IsSuccess is false)
            {
                return Result<TEntity?>.FailureConflict("A field that must contain a unique value in the table, contains a value that is already used.");
            }

            var errorMessage = $"The field '{tryGetDetailsResult.Data!.FieldName}' must have a unique value in the table '{tryGetDetailsResult.Data!.TableName}'. The value '{tryGetDetailsResult.Data!.KeyValue}' you are trying to save, is already used in a record in the table.";

            return Result<TEntity?>.FailureConflict(errorMessage);
        }
        catch (Exception ex)
        {
            RestoreSystemValues(record);

            string paramList = "(record, userId)";
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal("The record could not be updated.");
        }
    }
}
