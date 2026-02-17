using HandleConcurrency.Data;
using HandleConcurrency.Repositories.Interfaces;
using HandleConcurrencyBlazorDemo.Utils;
using HandleConcurrencyBlazorDemo.Utils.Interfaces;
using System.Buffers.Binary;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace HandleConcurrencyBlazorDemo.Data;

/// <summary>
/// Repository client that has external interface as a repository. It calls the REST API for data manipulation.
/// </summary>
/// <param name="logger"></param>
/// <param name="httpClient"></param>
/// <param name="configuration"></param>
/// <param name="queryVisitor"></param>
public class CustomerRepositoryClient(
    ILogger<CustomerRepositoryClient> logger,
    HttpClient httpClient,
    IConfiguration configuration,
    IUrlQueryVisitor<Customer> queryVisitor
    ) : IOptimisticConcurrencyRepository<Customer>
{
    // It is an old standard to put 'X-' in front of a custom header, and it is not nessecary to do so.
    private const string CustomHeaderRowVersionTag = "X-RowVersion";

    private const string RessourceName = "customers";
    private readonly string _ressourceUrl = configuration["Values:RESTHost"] + RessourceName;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Sends a Add request to REST API.
    /// </summary>
    /// <param name="entity">Record to add</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<Result<Customer?>> AddAsync(Customer entity, string userId)
    {
        string methodName = $"{nameof(AddAsync)}", paramList = "(entity, userId)";

        var url = $"{_ressourceUrl}";

        var httpResult = await httpClient.PostAsJsonAsync<Customer>(url, entity);
        var bodyString = await httpResult.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(bodyString))
        {
            var result = JsonSerializer.Deserialize<Result<Customer?>>(bodyString, jsonSerializerOptions);
            if (result is not null)
            {
                return result;
            }
        }

        logger.LogCritical("Error occured in '{methodName}{paramList}'. The error is: 'Call to Insert API returned an error. The code is {(int?)httpResult?.StatusCode} '{httpResult?.ReasonPhrase}''.",
            methodName,
            paramList,
            (int?)httpResult?.StatusCode, httpResult?.ReasonPhrase);
        return Result<Customer?>.MapFromStatusCode(
            httpResult is null ? HttpStatusCode.BadRequest : httpResult.StatusCode,
            "Could not send record to API.");
    }

    /// <summary>
    /// Sends a Delete request to REST API.
    /// </summary>
    /// <param name="entity">The customer to delete</param>
    /// <returns></returns>
    public async Task<Result> DeleteAsync(Customer entity)
    {
        string methodName = $"{nameof(DeleteAsync)}", paramList = $"(entity[Id={entity.Id}])";

        var url = $"{_ressourceUrl}/{entity.Id}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        // Set the concurrency stamp in the headers:
        var rowVersion = BinaryPrimitives.ReadUInt64BigEndian(entity!.Version);
        request.Headers.Add(CustomHeaderRowVersionTag, rowVersion.ToString());

        var httpResult = await httpClient.SendAsync(request).ConfigureAwait(false);
        var bodyString = await httpResult.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(bodyString))
        {
            var result = JsonSerializer.Deserialize<Result>(bodyString, jsonSerializerOptions);

            if (result is not null)
            {
                return result;
            }
        }

        logger.LogCritical("An error occured in '{methodName}{parmList}'. The error is: 'Call to Delete API returned an error. The code is {(int?)httpResult?.StatusCode} '{httpResult?.ReasonPhrase}''.",
            methodName,
            paramList,
            (int?)httpResult?.StatusCode, httpResult?.ReasonPhrase);
        return Result.MapFromStatusCode(
            httpResult is null ? HttpStatusCode.BadRequest : httpResult.StatusCode,
            "Could not send record to API.");
    }

    /// <summary>
    /// Send a GetAll request to REST API to get a 'page' of records.
    /// </summary>
    /// <param name="filter">Filter for selection</param>
    /// <param name="pageNumber">Page number to get</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns></returns>
    public async Task<Result<List<Customer>?>> GetAsync(Expression<Func<Customer, bool>>? filter, IEnumerable<SortDescriptor> sorting, int pageNumber, int pageSize)
    {
        string methodName = $"{nameof(GetAsync)}", paramList = $"(filter, sorting, {pageNumber}, {pageSize})";

        var buildUrlStringResult = BuildUrlString(filter, sorting, pageNumber, pageSize);
        if (buildUrlStringResult.IsSuccess is false)
        {
            return Result<List<Customer>?>.CopyResult(buildUrlStringResult);
        }

        string url = buildUrlStringResult.Data!;

        var httpResult = await httpClient.GetAsync(url);

        var bodyString = await httpResult.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(bodyString))
        {
            var contentType = httpResult.Content.Headers.ContentType?.MediaType;

            if (contentType == "application/json")
            {
                var result = JsonSerializer.Deserialize<Result<List<Customer>?>>(bodyString, jsonSerializerOptions);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        logger.LogCritical("An error occured in '{methodName}{parmList}'. The error is: 'Call to Get API returned an error. The code is {(int?)httpResult?.StatusCode} '{httpResult?.ReasonPhrase}''.",
            methodName,
            paramList,
            (int)httpResult.StatusCode, httpResult.ReasonPhrase);
        return Result<List<Customer>?>.Failure("Could not retrieve records from API.");
    }


    /// <summary>
    /// Sends a GetById request to REST API.
    /// </summary>
    /// <param name="id">Key of the record to get</param>
    /// <returns></returns>
    public async Task<Result<Customer?>> GetByIdAsync(long id)
    {
        string methodName = $"{nameof(GetByIdAsync)}", paramList = $"({id})";

        var url = $"{_ressourceUrl}/{id}";

        var httpResult = await httpClient.GetAsync(url);
        var bodyString = await httpResult.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(bodyString))
        {
            var result = JsonSerializer.Deserialize<Result<Customer?>>(bodyString, jsonSerializerOptions);
            if (result is not null)
            {
                return result;
            }
        }

        logger.LogCritical("An error occured in '{methodName}{parmList}'. The error is: 'Call to GetById API returned an error. The code is {(int?)httpResult?.StatusCode} '{httpResult?.ReasonPhrase}''.",
            methodName,
            paramList,
            (int?)httpResult?.StatusCode, httpResult?.ReasonPhrase);
        return Result<Customer?>.MapFromStatusCode(
            httpResult is null ? HttpStatusCode.BadRequest : httpResult.StatusCode,
            "Could not retrieve record from API.");
    }

    /// <summary>
    /// Sends a Update request to REST API.
    /// </summary>
    /// <param name="entity">The customer to update</param>
    /// <param name="userId">The id of the user</param>
    /// <returns></returns>
    public async Task<Result<Customer?>> UpdateAsync(Customer entity, string userId)
    {
        string methodName = $"{nameof(UpdateAsync)}", paramList = $"(entity[Id={entity.Id}], userId)";

        var url = $"{_ressourceUrl}/{entity.Id}";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(entity),
                Encoding.UTF8,
                "application/json")
        };
        var rowVersion = BinaryPrimitives.ReadUInt64BigEndian(entity!.Version);
        request.Headers.Add(CustomHeaderRowVersionTag, rowVersion.ToString());

        var httpResult = await httpClient.SendAsync(request);
        var bodyString = await httpResult.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(bodyString))
        {
            var result = JsonSerializer.Deserialize<Result<Customer?>>(bodyString, jsonSerializerOptions);
            if (result is not null)
            {
                return result;
            }
        }

        logger.LogCritical("An error occured in '{methodName}{parmList}'. The error is: 'Call to Update API returned an error. The code is {(int?)httpResult?.StatusCode} '{httpResult?.ReasonPhrase}''.",
            methodName,
            paramList,
            (int?)httpResult?.StatusCode, httpResult?.ReasonPhrase);
        return Result<Customer?>.MapFromStatusCode(
            httpResult is null ? HttpStatusCode.BadRequest : httpResult.StatusCode,
            "Could not send record to API.");
    }

    /// <summary>
    /// Add part of a query string to the master query string.
    /// </summary>
    /// <param name="queryStr">The master query string to add to</param>
    /// <param name="value">Query string to add to the master</param>
    private static void AddToQueryString(ref string queryStr, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            if (queryStr.Length is 0)
            {
                queryStr = value;
            }
            else
            {
                queryStr += "&" + value;
            }
        }
    }

    /// <summary>
    /// Builds the URL string.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="sorting"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    private Result<string?> BuildUrlString(Expression<Func<Customer, bool>>? filter, IEnumerable<SortDescriptor> sorting, int pageNumber, int pageSize)
    {
        var buildFromExpressionResult = queryVisitor.BuildFromExpression(filter);
        if (buildFromExpressionResult.IsSuccess is false)
        {
            return Result<string?>.CopyResult(buildFromExpressionResult);
        }
        var filterStr = buildFromExpressionResult.Data;

        var sortingStr = BuildFromSorting.Build(sorting);

        var url = _ressourceUrl;
        string queryStr = string.Empty;
        if (!string.IsNullOrEmpty(filterStr))
        {
            AddToQueryString(ref queryStr, filterStr);
        }

        if (!string.IsNullOrEmpty(sortingStr))
        {
            AddToQueryString(ref queryStr, sortingStr);
        }

        AddToQueryString(ref queryStr, $"pageNumber={pageNumber}&pageSize={pageSize}");

        url += "?" + queryStr;

        return Result<string?>.Success(url);
    }
}
