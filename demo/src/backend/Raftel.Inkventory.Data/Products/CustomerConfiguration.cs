using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Data.Extensions;
using Raftel.Inkventory.Core.Customers;

namespace Raftel.Inkventory.Data.Products;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ConfigureTypedId<Customer, CustomerId>();
        builder.Ignore(p => p.DomainEvents);

        builder.OwnsOne(c => c.Name, name =>
        {
            name.Property("_value")
                .HasColumnName("Name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.OwnsOne(c => c.FirstLastName, firstLastName =>
        {
            firstLastName.Property("_value")
                .HasColumnName("FirstLastName")
                .HasMaxLength(150)
                .IsRequired();
        });
    }
}