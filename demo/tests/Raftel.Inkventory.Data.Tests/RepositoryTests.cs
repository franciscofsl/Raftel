using Raftel.Core.UoW;
using Raftel.Inkventory.Core.Customers;
using Shouldly;

namespace Raftel.Inkventory.Data.Tests;

public class RepositoryTests(SqlServerTestContainerFixture fixture) : InkventoryDataTestBase(fixture)
{
    [Fact]
    public async Task AddCustomer_ShouldInsertCustomerSuccessfully()
    {
        var unitOfWork = GetService<IUnitOfWork>();
        var repository = unitOfWork.Repository<Customer, CustomerId>();

        var customer = new Customer(new Name("Luffy"), new FirstLastName("Monkey D."));
        customer.DomainEvents.ShouldHaveSingleItem();

        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        customer.DomainEvents.ShouldBeEmpty();

        var customers = await repository.GetAllAsync();
        customers.ShouldContain(customer);
    }

    [Fact]
    public async Task GetCustomerById_ShouldReturnCustomer_WhenCustomerExists()
    {
        var unitOfWork = GetService<IUnitOfWork>();
        var repository = unitOfWork.Repository<Customer, CustomerId>();

        var customer = new Customer(new Name("Luffy"), new FirstLastName("Monkey D."));
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        var customerFromDb = await repository.GetByIdAsync(customer.Id);
        customerFromDb.ShouldNotBeNull();
        customerFromDb.ShouldBeEquivalentTo(customer);
    }

    [Fact]
    public async Task GetAllCustomers_ShouldReturnAllCustomers()
    {
        var unitOfWork = GetService<IUnitOfWork>();
        var repository = unitOfWork.Repository<Customer, CustomerId>();

        var customer1 = new Customer(new Name("Luffy"), new FirstLastName("Monkey D."));
        var customer2 = new Customer(new Name("Zoro"), new FirstLastName("Roronoa"));
        await repository.AddAsync(customer1);
        await repository.AddAsync(customer2);
        await unitOfWork.SaveChangesAsync();

        var customers = await repository.GetAllAsync();
        customers.ShouldContain(c => c.Name.Equals("Luffy"));
        customers.ShouldContain(c => c.Name.Equals("Zoro"));
    }

    [Fact]
    public async Task UpdateCustomer_ShouldUpdateCustomerNameSuccessfully()
    {
        var unitOfWork = GetService<IUnitOfWork>();
        var repository = unitOfWork.Repository<Customer, CustomerId>();

        var customer = new Customer(new Name("Luffy"), new FirstLastName("Monkey D."));
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        customer.Rename(new Name("Zoro"));
        repository.Update(customer);
        await unitOfWork.SaveChangesAsync();

        var updatedCustomer = await repository.GetByIdAsync(customer.Id);
        updatedCustomer.ShouldNotBeNull();
        updatedCustomer.Name.ShouldBeEquivalentTo(new Name("Zoro"));
    }

    [Fact]
    public async Task RemoveCustomer_ShouldRemoveCustomerSuccessfully()
    {
        var unitOfWork = GetService<IUnitOfWork>();
        var repository = unitOfWork.Repository<Customer, CustomerId>();

        var customer = new Customer(new Name("Luffy"), new FirstLastName("Monkey D."));
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        repository.Remove(customer);
        await unitOfWork.SaveChangesAsync();

        var customers = await repository.GetAllAsync();
        customers.ShouldNotContain(customer);
    }
}
