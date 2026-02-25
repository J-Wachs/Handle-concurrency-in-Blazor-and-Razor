using HandleConcurrency.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HandleConcurrency.Repositories;

/// <summary>
/// Base abstraction for Repository classes that require setting a timeout for locks.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <param name="logger"></param>
/// <param name="databaseFactory"></param>
public class AbstractBaseRepositoryWithLockTimeout<TEntity>(
     ILogger<AbstractBaseRepositoryWithLockTimeout<TEntity>> logger,
     IDbContextFactory<DatabaseContext> databaseFactory) : AbstractBaseRepository<TEntity>(logger, databaseFactory)
        where TEntity : AbstractBaseTable
{
    protected virtual int OnGetLockTimeout()
    {
        // Miliseconds to wait for locks
        return 5000;
    }
}
