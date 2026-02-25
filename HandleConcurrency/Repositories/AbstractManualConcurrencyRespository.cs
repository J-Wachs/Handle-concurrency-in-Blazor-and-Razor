using HandleConcurrency.Data;
using HandleConcurrency.Helpers;
using HandleConcurrency.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace HandleConcurrency.Repositories;

/// <summary>
/// Base abstraction for repository where the conconcurrency handling is done manually (not by EF Core) by providing actions/functions that 
/// handle the concurrency stamp(s).
/// 
/// The class contains the method 'Update' that performs an update of the entire record, and handling of the concurency stamps are handled in 
/// the 'On*' methods. See 'ProductRepository.cs' for details.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <param name="logger"></param>
/// <param name="databaseFactory"></param>
public abstract class AbstractManualConcurrencyRespository<TEntity>(
    ILogger<AbstractManualConcurrencyRespository<TEntity>> logger,
    IDbContextFactory<DatabaseContext> databaseFactory) : AbstractBaseRepositoryWithLockTimeout<TEntity>(logger, databaseFactory), IRepository<TEntity>
        where TEntity : AbstractBaseTable
{
    /// <inheritdoc/>
    public async Task<Result<TEntity?>> AddAsync(TEntity record, string userId)
    {
        string methodName = $"{nameof(AddAsync)}", paramList = "(record, userId)";

        using var databaseContext = base.DatabaseFactory.CreateDbContext();

        try
        {
            StoreSystemValues(record);

            // Call to method to provide the record/sections concurrency stamp(s) before a new record is added:
            if (OnSetRowVersionBeforeAdd(record) is false)
            {
                logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: 'OnSetRowVersionBeforeAdd returned false'.",
                    methodName,
                    paramList);
                return Result<TEntity?>.Fatal("Could not add record to database.");
            }

            record.Id = 0;
            record.Created = DateTime.Now;
            record.CreatedBy = userId;

            _ = await databaseContext.Set<TEntity>()
                .AddAsync(record);
            await databaseContext.SaveChangesAsync();
            return Result<TEntity?>.Success(record);
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

            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal("Could not add record to database.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(TEntity record)
    {
        string methodName = $"{nameof(DeleteAsync)}", paramList = $"(record[Id={record.Id}])";

        using var databaseContext = base.DatabaseFactory.CreateDbContext();

        using var transaction = databaseContext.Database.BeginTransaction(IsolationLevel.RepeatableRead);
        try
        {
            var timeoutExpression = $"SET LOCK_TIMEOUT {OnGetLockTimeout()}";
            _ = await databaseContext.Database.ExecuteSqlRawAsync(timeoutExpression);

            var tableName = GetTableName(databaseContext);
            var sqlStatement = $"SELECT * FROM {tableName} WITH (XLOCK, ROWLOCK) WHERE ID = {{0}}";

            var databaseRecord = databaseContext.Set<TEntity>()
                .FromSqlRaw(sqlStatement, record.Id)
                .FirstOrDefault();

            if (databaseRecord is null)
            {
                await transaction.RollbackAsync();
                Logger.LogError("Error occured in '{methodName}{paramList}'. The error is: 'There is no existing record in {tableName} with key {record.Id}'.",
                    methodName,
                    paramList,
                    tableName,
                    record);
                return Result.FailureNotFound("Could not add record to database.");
            }

            // Call to method that validates that the concurrency stamp(s) of the record in table match the one provided
            // as parameter to this method:
            if (OnValidateConcurrencyStampAfterRead(record, databaseRecord) is false)
            {
                await transaction.RollbackAsync();
                var databaseModifiedBy = databaseRecord.ModifiedBy;
                var databaseModified = databaseRecord.Modified;
                return Result.FailureConflict($"The data could not be stored because the user '{databaseModifiedBy}' has stored changed data on the '{databaseModified}' which is after you read the data.");
            }

            _ = databaseContext.Set<TEntity>().Remove(record);
            await databaseContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result.Fatal("Could not add record to database.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<TEntity?>> UpdateAsync(TEntity record, string userId)
    {
        string methodName = $"{nameof(UpdateAsync)}";

        try
        {
            StoreSystemValues(record);

            // Setup inline action:
            void MapEntireRecord(TEntity databaseRecord)
            {
                // Assign all fields but Id:Kan du 
                PropertyCopier.Copy(record, databaseRecord);
            }

            // Setup inline functions:
            bool ValidateAfterRead(TEntity databaseRecord)
            {
                return OnValidateConcurrencyStampAfterRead(record, databaseRecord);
            }

            bool SetBeforeUpdate(TEntity databaseRecord)
            {
                return OnSetConcurrencyStampBeforeUpdate(record, databaseRecord);
            }

            var updateRowResult = await UpdateRowAsync(record.Id, MapEntireRecord, ValidateAfterRead, SetBeforeUpdate, userId);
            if (updateRowResult.IsSuccess is false)
            {
                RestoreSystemValues(record);
            }
            return updateRowResult;
        }
        catch (Exception ex)
        {
            RestoreSystemValues(record);

            string paramList = $"(record[Id={record.Id}], userId)";
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal("Could not update record.");
        }
    }

    #region Virtual (On*) methods

    // In your own implementations, you must first inherit from this abstact class, and then
    // provide 'override' methods that assign and tests the concurrency stamp(s).
    // See the 'ProductRepository' for an example.

    public virtual bool OnSetConcurrencyStampBeforeUpdate(TEntity record, TEntity databaseRecord)
    {
        throw new NotImplementedException("You must implement a override method for this");
    }

    public virtual bool OnSetRowVersionBeforeAdd(TEntity record)
    {
        throw new NotImplementedException("You must implement a override method for this");
    }

    public virtual bool OnValidateConcurrencyStampAfterRead(TEntity record, TEntity databaseRecord)
    {
        throw new NotImplementedException("You must implement a override method for this");
    }

    #endregion Virtual (On*) methods

    /// <summary>
    /// TODO: Documentation!
    /// </summary>
    /// <param name="id"></param>
    /// <param name="fieldMapping"></param>
    /// <param name="validateAfterRead"></param>
    /// <param name="setBeforeUpdate"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    protected async Task<Result<TEntity?>> UpdateRowAsync(
        long id,
        Action<TEntity> fieldMapping,
        Func<TEntity, bool> validateAfterRead,
        Func<TEntity, bool> setBeforeUpdate,
        string userId)
    {
        string methodName = $"{nameof(UpdateRowAsync)}", paramList = $"({id}, fieldMapping, validateAfterRead, setBeforeUpdate, userId)";

        try
        {
            using var databaseContext = base.DatabaseFactory.CreateDbContext();
            using var transaction = databaseContext.Database.BeginTransaction(IsolationLevel.RepeatableRead);
            try
            {
                var timeoutExpression = $"SET LOCK_TIMEOUT {OnGetLockTimeout()}";
                _ = await databaseContext.Database.ExecuteSqlRawAsync(timeoutExpression);

                var sqlStatement = $"SELECT * FROM {GetTableName(databaseContext)} WITH (XLOCK, ROWLOCK) WHERE ID = {{0}}";

                var databaseRecord = databaseContext.Set<TEntity>()
                    .FromSqlRaw(sqlStatement, id)
                    .FirstOrDefault();

                // This will occur if somebody has deleted the record:
                if (databaseRecord is null)
                {
                    await transaction.RollbackAsync();
                    Logger.LogError("Error occured in '{methodName}{paramList}'. The error is: 'There is no existing record in {GetTableName(databaseContext)} with key {id}'.",
                        methodName,
                        paramList,
                        GetTableName(databaseContext),
                        id);
                    return Result<TEntity?>.FailureNotFound("Could not find the record to update in the database.");
                }

                // Manual check that the concurrency stamp is still the same as in the input record:
                if (validateAfterRead(databaseRecord) is false)
                {
                    await transaction.RollbackAsync();
                    var databaseModifiedBy = databaseRecord.ModifiedBy;
                    var databaseModified = databaseRecord.Modified;
                    return Result<TEntity?>.FailureConflict($"The data could not be stored because the user '{databaseModifiedBy}' has stored changed data on the '{databaseModified}' which is after you read the data.");
                }

                // Manual update of the concurrency stamp(s):
                if (setBeforeUpdate(databaseRecord) is false)
                {
                    await transaction.RollbackAsync();
                    Logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: 'OnSetRowVersionBeforeUpdate returned false'.",
                        methodName,
                        paramList);
                    return Result<TEntity?>.Fatal("Could not update record in database.");
                }

                // Mapping of fields from the input record to the database record:
                fieldMapping(databaseRecord);

                // In this implementation, no matter what section is changed, the fields 'Modified' and 'ModifiedBy'
                // are used. If you want, you could add sets of 'Modified'/'ModifiedBy' per section.
                databaseRecord.Modified = DateTime.Now;
                databaseRecord.ModifiedBy = userId;

                _ = databaseContext.Set<TEntity>().Update(databaseRecord);
                await databaseContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result<TEntity?>.Success(databaseRecord);
            }
            // If this is a doublicate key violation, inform the user
            catch (DbUpdateException ex) when (ex.InnerException is not null &&
                                                ex.GetBaseException() is SqlException &&
                                                ((SqlException)ex.InnerException).Number is (int)SqlErrors.ViolationUniqueKeyConstraint)
            {
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
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }
                logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
                return Result<TEntity?>.Fatal("Could not update record in the database.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<TEntity?>.Fatal("Could not update record in the database.");
        }
    }
}
