using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raftel.Core.BaseTypes;

namespace Raftel.Data.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<TId> ConfigureTypedId<TEntity, TId>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
        where TId : TypedId<Guid>
    {
        var entityType = typeof(TEntity);
        var entityName = entityType.Name;

        var idProperty = entityType.GetProperty("Id");

        if (idProperty is null)
            throw new InvalidOperationException(
                $"No se encontró una propiedad 'Id' en el tipo '{entityType.FullName}'.");

        var propertyBuilder = builder
            .Property<TId>("Id")
            .HasConversion(
                id => (Guid)(id!),
                value => (TId)Activator.CreateInstance(typeof(TId), (object)value)!)
            .ValueGeneratedNever();

        propertyBuilder.Metadata.SetColumnName($"{entityName}Id");

        return propertyBuilder;
    }
}