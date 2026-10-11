using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Raftel.Application;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Outbox;
using Raftel.Domain.Outbox.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// EF Core backed <see cref="IOutboxStore"/>. Claiming is one atomic provider-specific statement that
/// leases the rows and counts the attempt, so several processors never receive the same message.
/// </summary>
/// <remarks>
/// The claim statement is executed directly on the connection, not through <c>FromSql</c>: both
/// <c>UPDATE ... OUTPUT</c> (SQL Server) and <c>UPDATE ... RETURNING</c> (PostgreSQL) must be the outermost
/// statement, and EF would wrap raw SQL in a subquery. Every query ignores the global query filters: the
/// processor serves all tenants.
/// </remarks>
internal sealed class OutboxStore<TDbContext> : IOutboxStore
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly TDbContext _context;
    private readonly OutboxOptions _options;
    private readonly TimeProvider _timeProvider;

    public OutboxStore(TDbContext context, OutboxOptions options, TimeProvider timeProvider)
    {
        _context = context;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var ids = await ExecuteClaimAsync(now, cancellationToken);

        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.OutboxMessage
            .IgnoreQueryFilters()
            .Where(message => ids.Contains(message.Id))
            .OrderBy(message => message.OccurredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries)
            {
                entry.State = EntityState.Detached;
            }

            return false;
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetDeadLettersAsync(CancellationToken cancellationToken = default)
    {
        var maxAttempts = _options.MaxAttempts;

        return await _context.OutboxMessage
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(message => message.ProcessedOn == null && message.AttemptCount >= maxAttempts)
            .OrderBy(message => message.OccurredOn)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        _context.OutboxMessage
            .IgnoreQueryFilters()
            .Where(message => message.ProcessedOn != null && message.ProcessedOn < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    private async Task<List<OutboxMessageId>> ExecuteClaimAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = BuildClaimSql();
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            AddParameter(command, "now", now);
            AddParameter(command, "lockedUntil", now + _options.LockDuration);
            AddParameter(command, "maxAttempts", _options.MaxAttempts);
            AddParameter(command, "batchSize", _options.BatchSize);

            var ids = new List<OutboxMessageId>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(new OutboxMessageId(reader.GetGuid(0)));
            }

            return ids;
        }
        finally
        {
            if (openedHere)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    private string BuildClaimSql()
    {
        var entityType = _context.Model.FindEntityType(typeof(OutboxMessage))!;
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        string Column(string property) => entityType.FindProperty(property)!.GetColumnName(table)!;

        return _context.Database.IsNpgsql()
            ? BuildPostgreSqlClaim(entityType, Column)
            : BuildSqlServerClaim(entityType, Column);
    }

    private static string BuildSqlServerClaim(IEntityType entityType, Func<string, string> column)
    {
        string Q(string name) => $"[{name.Replace("]", "]]")}]";

        var tableName = QualifiedName(entityType, Q);
        var sql = new StringBuilder();
        sql.Append($"WITH Claimable AS (SELECT TOP (@batchSize) * FROM {tableName} WITH (UPDLOCK, READPAST, ROWLOCK) ");
        sql.Append($"WHERE {Q(column(nameof(OutboxMessage.ProcessedOn)))} IS NULL ");
        sql.Append($"AND {Q(column(nameof(OutboxMessage.AttemptCount)))} < @maxAttempts ");
        sql.Append($"AND ({Q(column(nameof(OutboxMessage.NextAttemptOn)))} IS NULL OR {Q(column(nameof(OutboxMessage.NextAttemptOn)))} <= @now) ");
        sql.Append($"AND ({Q(column(nameof(OutboxMessage.LockedUntil)))} IS NULL OR {Q(column(nameof(OutboxMessage.LockedUntil)))} < @now) ");
        sql.Append($"ORDER BY {Q(column(nameof(OutboxMessage.OccurredOn)))}) ");
        sql.Append($"UPDATE Claimable SET {Q(column(nameof(OutboxMessage.LockedUntil)))} = @lockedUntil, ");
        sql.Append($"{Q(column(nameof(OutboxMessage.AttemptCount)))} = {Q(column(nameof(OutboxMessage.AttemptCount)))} + 1 ");
        sql.Append($"OUTPUT inserted.{Q(column(nameof(OutboxMessage.Id)))};");
        return sql.ToString();
    }

    private static string BuildPostgreSqlClaim(IEntityType entityType, Func<string, string> column)
    {
        string Q(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

        var tableName = QualifiedName(entityType, Q);
        var id = Q(column(nameof(OutboxMessage.Id)));
        var sql = new StringBuilder();
        sql.Append($"UPDATE {tableName} SET {Q(column(nameof(OutboxMessage.LockedUntil)))} = @lockedUntil, ");
        sql.Append($"{Q(column(nameof(OutboxMessage.AttemptCount)))} = {Q(column(nameof(OutboxMessage.AttemptCount)))} + 1 ");
        sql.Append($"WHERE {id} IN (SELECT {id} FROM {tableName} ");
        sql.Append($"WHERE {Q(column(nameof(OutboxMessage.ProcessedOn)))} IS NULL ");
        sql.Append($"AND {Q(column(nameof(OutboxMessage.AttemptCount)))} < @maxAttempts ");
        sql.Append($"AND ({Q(column(nameof(OutboxMessage.NextAttemptOn)))} IS NULL OR {Q(column(nameof(OutboxMessage.NextAttemptOn)))} <= @now) ");
        sql.Append($"AND ({Q(column(nameof(OutboxMessage.LockedUntil)))} IS NULL OR {Q(column(nameof(OutboxMessage.LockedUntil)))} < @now) ");
        sql.Append($"ORDER BY {Q(column(nameof(OutboxMessage.OccurredOn)))} LIMIT @batchSize FOR UPDATE SKIP LOCKED) ");
        sql.Append($"RETURNING {id};");
        return sql.ToString();
    }

    private static string QualifiedName(IEntityType entityType, Func<string, string> quote)
    {
        var schema = entityType.GetSchema();
        var table = quote(entityType.GetTableName()!);
        return schema is null ? table : $"{quote(schema)}.{table}";
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        if (value is DateTimeOffset)
        {
            parameter.DbType = DbType.DateTimeOffset;
        }

        command.Parameters.Add(parameter);
    }
}
