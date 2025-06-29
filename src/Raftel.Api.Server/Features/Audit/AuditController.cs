using Microsoft.AspNetCore.Mvc;
using Raftel.Application.Features.Audit;
using Raftel.Application.Queries;

namespace Raftel.Api.Server.Features.Audit;

/// <summary>
/// API controller for audit-related operations.
/// </summary>
[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher;

    public AuditController(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
    }

    /// <summary>
    /// Gets the audit history for a specific entity.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit history for the entity.</returns>
    [HttpGet("{entityName}/{entityId}")]
    public async Task<IActionResult> GetEntityAuditHistory(
        string entityName, 
        string entityId, 
        CancellationToken cancellationToken = default)
    {
        var query = new GetEntityAuditHistoryQuery(entityName, entityId);
        var result = await _queryDispatcher.DispatchAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
    }
}

/// <summary>
/// Example usage for pirate entities specifically.
/// </summary>
[ApiController]
[Route("api/pirates")]
public class PirateAuditController : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher;

    public PirateAuditController(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
    }

    /// <summary>
    /// Gets the audit history for a specific pirate.
    /// Example: GET /api/pirates/123/audit
    /// </summary>
    /// <param name="pirateId">The pirate identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit history for the pirate.</returns>
    [HttpGet("{pirateId}/audit")]
    public async Task<IActionResult> GetPirateAuditHistory(
        string pirateId, 
        CancellationToken cancellationToken = default)
    {
        var query = new GetEntityAuditHistoryQuery("Pirate", pirateId);
        var result = await _queryDispatcher.DispatchAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { Error = result.Error.Code, Message = result.Error.Message });
    }
}