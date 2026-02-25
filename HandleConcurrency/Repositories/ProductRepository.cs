using HandleConcurrency.Data;
using HandleConcurrency.Data.DTOs;
using HandleConcurrency.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandleConcurrency.Repositories;

/// <summary>
/// This repository class for the product table shows how to handle concurrency check for subset of fields in a
/// table. All the needed code is here, making it somewhat simple to implement concurrency by sections of fields.
/// </summary>
/// <param name="logger"></param>
/// <param name="databaseFactory"></param>
public class ProductRepository(
    ILogger<ProductRepository> logger, IDbContextFactory<DatabaseContext> databaseFactory
    ) : AbstractManualConcurrencyRespository<Product>(logger, databaseFactory), IProductRepository
{
    private const int InitialVersion = 1;
    private const int VersionIncreaseCount = 1;

    #region Event methods for updating the complete record

    /// <summary>
    /// This override of the base class method, contains the code to set the initial value of the concurrency stamps
    /// when adding a record.
    /// </summary>
    /// <param name="record">Record to be added to table</param>
    /// <returns>True if assignment if initial values as succesful. Otherwise False</returns>
    public override bool OnSetRowVersionBeforeAdd(Product record)
    {
        record.VersionInfo = InitialVersion;
        record.VersionQuantities = InitialVersion;
        return true;
    }

    /// <summary>
    /// This override of the base class method, does the checking of the concurrency stamps in order
    /// to ensure that the record in the database has not been updated by another
    /// user since it was read. The method is called during the execution of the 'Update' method
    /// (update of all fields in record, not a specific section).
    /// </summary>
    /// <param name="record">The record contains the updated fields</param>
    /// <param name="databaseRecord">The record in the table</param>
    /// <returns>True if Comcurrency Stamp in database matches record in memory. Otherwise False</returns>
    public override bool OnValidateConcurrencyStampAfterRead(Product record, Product databaseRecord)
    {
        if (record.VersionInfo != databaseRecord.VersionInfo ||
            record.VersionQuantities != databaseRecord.VersionQuantities)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This override of the base class method, contains the code to update all the concurrency stamps during 
    /// executions of the method 'Update' (update of all fields in record, not a specific section).
    /// </summary>
    /// <param name="record">The record contains the updated fields. This record contains the data to update the record in the database</param>
    /// <param name="databaseRecord">The record in the table</param>
    /// <returns>True if update of Concurrency Stamp was succesful. Otherwise alse</returns>
    public override bool OnSetConcurrencyStampBeforeUpdate(Product record, Product databaseRecord)
    {
        record.VersionInfo = databaseRecord.VersionInfo + VersionIncreaseCount;
        record.VersionQuantities = databaseRecord.VersionQuantities + VersionIncreaseCount;

        return true;
    }

    #endregion Event methods for updating the complete record

    #region Update of sections

    // Below here, you will find the specific methods to update sections of the record.
    // In this example, there are two section update methods: One for updating Info about
    // a product, and one for updating the Quantifies on a product.

    /// <inheritdoc />
    public async Task<Result<ProductInfoDTO?>> UpdateInfoAsync(ProductInfoDTO record, string userId)
    {
        string methodName = $"{nameof(UpdateInfoAsync)}";

        try
        {
            // Inline field mapping action to be passed to 'UpdateRow':
            void MapRecord(Product databaseRecord)
            {
                PropertyCopier.Copy(record, databaseRecord);
            }

            // Inline validation function to be passed to 'UpdateRow':
            bool ValidateConcurrencyStampAfterRead(Product databaseRecord)
            {
                if (record.VersionInfo != databaseRecord.VersionInfo)
                {
                    return false;
                }
                return true;
            }

            // Inline update concurrency stamp function to be passed to 'UpdateRow':
            bool SetConcurrencyStampBeforeUpdate(Product databaseRecord)
            {
                record.VersionInfo++;
                // You must update the VersionInfo concurrency stamp from the 'record' to the 'databaseRecord', as it 
                // is not copied over by the 'MergeTo()' method:
                databaseRecord.VersionInfo = record.VersionInfo;

                return true;
            }

            var result = await UpdateRowAsync(record.Id, MapRecord, ValidateConcurrencyStampAfterRead, SetConcurrencyStampBeforeUpdate, userId);
            if (result.IsSuccess is false)
            {
                // Using CopyResult, we get the right result code back as well as the messages.
                return Result<ProductInfoDTO?>.CopyResult(result);
            }

            // Due to the defined '...implicit operator...' in the DTO class, we can pass the returned record that
            // is a Product class.
            return Result<ProductInfoDTO?>.Success(result.Data!);
        }
        catch (Exception ex)
        {
            string paramList = "(record, userId)";
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<ProductInfoDTO?>.Fatal("Could not update the record.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ProductQtyDTO?>> UpdateQuantitiesAsync(ProductQtyDTO record, string userId)
    {
        string methodName = $"{nameof(UpdateQuantitiesAsync)}";

        try
        {
            // Inline field mapping action to be passed to 'UpdateRow':
            void MapRecord(Product databaseRecord)
            {
                PropertyCopier.Copy(record, databaseRecord);
            }

            // Inline validation function to be passed to 'UpdateRow':
            bool ValidateConcurrencyStampAfterRead(Product databaseRecord)
            {
                if (record.VersionQuantities != databaseRecord.VersionQuantities)
                {
                    return false;
                }
                return true;
            }

            // Inline update concurrency stamp function to be passed to 'UpdateRow':
            bool SetConcurrencyStampBeforeUpdate(Product databaseRecord)
            {
                record.VersionQuantities++;
                // You must update the VersionQuantities concurrency stamp from the 'record' to the 'databaseRecord', as it 
                // is not copied over by the 'MergeTo()' method:
                databaseRecord.VersionQuantities = record.VersionQuantities;

                return true;
            }

            var result = await UpdateRowAsync(record.Id, MapRecord, ValidateConcurrencyStampAfterRead, SetConcurrencyStampBeforeUpdate, userId);
            if (result.IsSuccess is false)
            {
                // Using CopyResult, we get the right result code back as well as the messages.
                return Result<ProductQtyDTO?>.CopyResult(result);
            }

            // Due to the defined '...implicit operator...' in the DTO class, we can pass the returned record that
            // is a Product class.
            return Result<ProductQtyDTO?>.Success(result.Data!);
        }
        catch (Exception ex)
        {
            string paramList = "(record, userId)";
            logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: '{ex}'.", methodName, paramList, ex);
            return Result<ProductQtyDTO?>.Fatal("Could not update the record.");
        }
    }

    #endregion Update of sections
}
