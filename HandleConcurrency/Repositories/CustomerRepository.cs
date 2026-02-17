using HandleConcurrency.Data;
using HandleConcurrency.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandleConcurrency.Repositories;

/// <summary>
/// Example repository that provide optimistic concurrency check for the Customer table.
/// 
/// This shows how simple it actually is when the functionality is located in the abstract respoitory class.
/// </summary>
/// <param name="logger"></param>
/// <param name="databaseFactory"></param>
public class CustomerRepository(
    ILogger<CustomerRepository> logger,
    IDbContextFactory<DatabaseContext> databaseFactory
    ): AbstractOptimisticConcurrencyRepository<Customer>(logger, databaseFactory), IOptimisticConcurrencyRepository<Customer>
{
}
