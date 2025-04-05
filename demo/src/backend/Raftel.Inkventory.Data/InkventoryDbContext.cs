using Microsoft.EntityFrameworkCore;
using Raftel.Data.DbContexts;
using Raftel.Inkventory.Core.Customers;

namespace Raftel.Inkventory.Data;

public class InkventoryDbContext : RaftelDbContext<InkventoryDbContext>
{
    public InkventoryDbContext(DbContextOptions<InkventoryDbContext> options)
        : base(options)
    {
    } 
    
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InkventoryDbContext).Assembly);
    }
}
