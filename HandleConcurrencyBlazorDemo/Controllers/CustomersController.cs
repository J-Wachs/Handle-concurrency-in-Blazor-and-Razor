using HandleConcurrency.Data;
using HandleConcurrency.Repositories.Interfaces;
using HandleConcurrencyBlazorDemo.Controllers.DTOs;
using HandleConcurrencyBlazorDemo.Controllers.Utils.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Buffers.Binary;

namespace HandleConcurrencyBlazorDemo.Controllers;

/// <summary>
/// Controller for demoing how to use optimistic concurrency check in an REST API.
/// 
/// Focus is on concurrency, and the because of this, authority is NOT implemented.
/// </summary>
/// <param name="logger"></param>
/// <param name="repository"></param>
/// <param name="queryParser"></param>
[ApiController]
[Route("api/[controller]")]
// Note the usage of AsIResult() of the Result class!
// No matter the result of the call to a method, the reply returned to called
// is the correct one (Ok, NotFound, Conflict, Server Error etc.) because the method 
// AsIResult() sets the http status code, and the return obejct.

public class CustomersController(
    ILogger<CustomersController> logger,
    [FromKeyedServices("customerrepository")] IOptimisticConcurrencyRepository<Customer> repository,
    IAPIQueryParser<Customer> queryParser
    ) : ControllerBase
{
    private const int FirstPage = 1;
    private const int DefaultPageSize = 25;
    // Yes, I know I do not *have to* put 'X-' in front of a custom header, but I do.
    private const string CustomHeaderRowVersionTag = "X-RowVersion";

    private const string HeaderLocation = "Location";

    // In this implementation we only allow filtering on the fields Id and Name.
    private readonly List<string> AllowedProperties = [nameof(Customer.Id), nameof(Customer.Name)];

    /// <summary>
    /// Add a new Customer record.
    /// </summary>
    /// <param name="customer">The record to create</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Customer?>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<Customer?>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<Customer?>))]
    public async Task<IResult> AddAsync(CustomerAddUpdateDTO customerDTO)
    {
        // You could actually just pass 'customerDTO' as parameter to AddAsync,
        // but I put the assignment below for clarity.
        Customer customer = customerDTO;
        var result = await repository.AddAsync(customer, "API user name");
        if (result.IsSuccess)
        {
            // Here I just get 'this' server's name from the url and
            // use it for server name. You might have to adjust this:
            var location = $"{Request.Scheme}://{Request.Host}{Request.Path}/{result.Data!.Id}";

            Request.Headers[HeaderLocation] = location;
        }
        return result.AsIResult();
    }

    /// <summary>
    /// Delete a record.
    /// </summary>
    /// <param name="id">Key of the record to delete.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<Customer>))]
    public async Task<IResult> DeleteAsync(long id)
    {
        string methodName = $"{nameof(DeleteAsync)}", paramList = $"({id})";

        // Setup class to be able to delete
        if (Request.Headers.TryGetValue(CustomHeaderRowVersionTag, out StringValues headerValues))
        {
            // We need to get, convert and put in place the concurrency stamp:
            if (ulong.TryParse(headerValues.ToString(), out ulong headerValue))
            {
                byte[] rowVersion = new byte[8];
                // Write value into rowVersion field
                BinaryPrimitives.WriteUInt64BigEndian(rowVersion, headerValue);

                var resultDelete = await repository.DeleteAsync(new Customer() { Id = id, Version = rowVersion });

                return resultDelete.AsIResult();
            }
        }

        logger.LogError("Error occured in '{methodName}{paramList}'. The error is: 'X-RowVersion header is missing or not numeric: '{headerValues}''.",
            methodName,
            paramList,
            headerValues);
        return Result.FailureBadRequest("Data is missing in order to perform delete of row.").AsIResult();
    }

    /// <summary>
    /// Gets a list of records
    /// </summary>
    /// <param name="sort">The descriptors of the sorting</param>
    /// <param name="pageNumber">Page number to start at</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<List<Customer>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<List<Customer>>))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(Result<List<Customer>>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<List<Customer>>))]
    public async Task<IResult> GetAllAsync([FromQuery] string? sort, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        queryParser.SetAllowedFields(AllowedProperties);

        // As runtime cannot parse the special structure (field[comparator]=value) of the filtering we let ParseFilter do it:
        var filterExpressionResult = queryParser.ParseFilter(Request.Query);
        if (filterExpressionResult.IsSuccess is false)
        {
            return filterExpressionResult.AsIResult();
        }

        var sortDescriptorsResult = queryParser.ParseSort(sort);
        if (sortDescriptorsResult.IsSuccess is false)
        {
            return sortDescriptorsResult.AsIResult();
        }

        pageNumber ??= FirstPage;
        pageSize ??= DefaultPageSize;
        
        var result = await repository.GetAsync(filterExpressionResult.Data!, sortDescriptorsResult.Data!, pageNumber.Value, pageSize.Value);
        if (result.IsSuccess)
        {
            List<CustomerDTO> listOfCustomers = [];
            foreach(var customer in result.Data!)
            {
                // The '...implicit operator...' in the DTO class will
                // convert from Customer class to CustomerDTO:
                listOfCustomers.Add(customer);
            }
            return Result<List<CustomerDTO>?>.Success(listOfCustomers).AsIResult();
        }
       
        return Result<List<CustomerDTO>?>.CopyResult(result).AsIResult();
    }

    /// <summary>
    /// Gets one record with given key
    /// </summary>
    /// <param name="id">Key of record to retrieve</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<Customer>))]
    public async Task<IResult> GetByIdAsync(long id)
    {
        var result = await repository.GetByIdAsync(id);
        if (result.IsSuccess)
        {
            // The '...implicit operator...' in the DTO will convert 
            // from the 'Customer' to the DTO.
            return Result<CustomerDTO?>.Success(result.Data!).AsIResult();
        }

        return Result<CustomerDTO?>.CopyResult(result).AsIResult();
    }

    /// <summary>
    /// Update a record
    /// </summary>
    /// <param name="id">Key of the record to update</param>
    /// <param name="customerDTO">The record data</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(Result<Customer>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<Customer>))]
    public async Task<IResult> UpdateAsync(long id, CustomerAddUpdateDTO customerDTO)
    {
        string methodName = $"{nameof(UpdateAsync)}", paramList = $"({id}, customerDTO)";

        if (Request.Headers.TryGetValue(CustomHeaderRowVersionTag, out StringValues headerValues))
        {
            // We need to get, convert and put in place the concurrency stamp:
            if (ulong.TryParse(headerValues.ToString(), out ulong headerValue))
            {
                // In order to update, it is nessecary to get the record first:
                var getByIdResult = await repository.GetByIdAsync(id);
                if (getByIdResult.IsSuccess is false)
                {
                    return getByIdResult.AsIResult();
                }

                var fetchedCustomer = getByIdResult.Data!;
                // The method '...implicit operator...' in the DTO class,
                // is called and does the acual mapping:
                customerDTO.MergeTo(fetchedCustomer);
                BinaryPrimitives.WriteUInt64BigEndian(fetchedCustomer.Version, headerValue);

                var updateResult = await repository.UpdateAsync(fetchedCustomer, "API user name");
                return updateResult.AsIResult();
            }
        }

        logger.LogError("Error occured in '{methodName}{paramList}'. The error is: 'X-RowVersion header is missing or is not numeric: '{headerValues}''.",
            methodName,
            paramList,
            headerValues);
        return Result.FailureBadRequest("Data is missing in order to perform update of data").AsIResult();
    }
}
