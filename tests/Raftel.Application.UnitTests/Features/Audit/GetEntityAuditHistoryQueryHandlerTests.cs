using NSubstitute;
using Raftel.Application.Features.Audit;
using Raftel.Infrastructure.Data.Audit;
using Raftel.Infrastructure.Data.Repositories.Audit;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Audit;

public class GetEntityAuditHistoryQueryHandlerTests
{
    private readonly IAuditRepository _auditRepository;
    private readonly GetEntityAuditHistoryQueryHandler _handler;

    public GetEntityAuditHistoryQueryHandlerTests()
    {
        _auditRepository = Substitute.For<IAuditRepository>();
        _handler = new GetEntityAuditHistoryQueryHandler(_auditRepository);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenAuditEntriesExist()
    {
        // Arrange
        var query = new GetEntityAuditHistoryQuery("TestEntity", "123");
        var auditEntry = AuditEntry.Create(AuditChangeType.Update, "TestEntity", "123");
        auditEntry.AddPropertyChange("Name", "OldValue", "NewValue");
        
        var auditEntries = new List<AuditEntry> { auditEntry };

        _auditRepository
            .GetEntityAuditHistoryAsync(query.EntityName, query.EntityId, Arg.Any<CancellationToken>())
            .Returns(auditEntries);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        
        var dto = result.Value.First();
        dto.ChangeType.ShouldBe(AuditChangeType.Update);
        dto.EntityName.ShouldBe("TestEntity");
        dto.EntityId.ShouldBe("123");
        dto.Changes.Count.ShouldBe(1);
        dto.Changes.First().PropertyName.ShouldBe("Name");
        dto.Changes.First().OldValue.ShouldBe("OldValue");
        dto.Changes.First().NewValue.ShouldBe("NewValue");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenNoAuditEntriesExist()
    {
        // Arrange
        var query = new GetEntityAuditHistoryQuery("TestEntity", "456");
        var auditEntries = new List<AuditEntry>();

        _auditRepository
            .GetEntityAuditHistoryAsync(query.EntityName, query.EntityId, Arg.Any<CancellationToken>())
            .Returns(auditEntries);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var query = new GetEntityAuditHistoryQuery("TestEntity", "789");

        _auditRepository
            .GetEntityAuditHistoryAsync(query.EntityName, query.EntityId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("AuditHistory.QueryFailed");
        result.Error.Message.ShouldContain("Failed to retrieve audit history");
    }
}