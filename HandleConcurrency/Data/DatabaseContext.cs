using Microsoft.EntityFrameworkCore;

namespace HandleConcurrency.Data;

/// <summary>
/// Database context for abstractions and demos.
/// </summary>
/// <param name="options"></param>
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            // Create a unique index for Bank Account
            // The purpose of this is, to demonstrate that the repository abstraction 
            // handles the exception from EF Core, if the same Bank Account is used
            // more than once.
            entity.HasIndex(e => e.BankAccount)
                  .IsUnique()
                  .HasDatabaseName("IX_Customers_BankAccount");
        });
    }
}
