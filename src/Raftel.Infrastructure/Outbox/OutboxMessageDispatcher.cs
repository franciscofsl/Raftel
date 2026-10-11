using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.DomainEvents;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Outbox;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Delivers one outbox message to every handler registered for its event.
/// </summary>
/// <remarks>
/// Each handler runs in its own DI scope and its own transaction, under the tenant and correlation
/// identifier captured with the message. The handler's database writes and the receipt that it completed
/// commit together, so a redelivered message skips handlers that already finished and a failed handler
/// leaves no partial writes. Handlers after a failing one are not attempted in the same delivery.
/// </remarks>
internal sealed class OutboxMessageDispatcher(IServiceScopeFactory scopeFactory, IOutboxSerializer serializer)
{
    public async Task<DeliveryOutcome> DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var deserialized = serializer.Deserialize(message.Type, message.Content);

        if (deserialized.IsFailure)
        {
            return DeliveryOutcome.Failed(deserialized.Error.Message);
        }

        var domainEvent = deserialized.Value;
        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

        for (var index = 0;; index++)
        {
            var outcome = await DeliverToHandlerAsync(message, domainEvent, handlerInterface, index, cancellationToken);

            if (outcome is null)
            {
                return DeliveryOutcome.Success;
            }

            if (!outcome.Succeeded)
            {
                return outcome;
            }
        }
    }

    /// <returns>
    /// <see langword="null"/> when there is no handler at <paramref name="index"/>, meaning every handler
    /// has been dealt with.
    /// </returns>
    private async Task<DeliveryOutcome> DeliverToHandlerAsync(
        OutboxMessage message,
        IDomainEvent domainEvent,
        Type handlerInterface,
        int index,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var handlers = services.GetServices(handlerInterface).ToList();
        if (index >= handlers.Count)
        {
            return null;
        }

        var handler = handlers[index]!;
        var handlerName = HandlerName(handler);

        using var tenant = services.GetRequiredService<ICurrentTenant>().Change(message.TenantId);
        using var correlation = services.GetRequiredService<ICorrelationScope>().Begin(message.CorrelationId);

        var idempotency = services.GetRequiredService<IIdempotencyStore>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        ITransaction transaction = null;

        try
        {
            if (await idempotency.HasCompletedAsync(message.Id, handlerName, cancellationToken))
            {
                return DeliveryOutcome.Success;
            }

            transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            await InvokeAsync(handlerInterface, handler, domainEvent, cancellationToken);
            await idempotency.MarkCompletedAsync(message.Id, handlerName, cancellationToken);
            await transaction.CommitAsync(CancellationToken.None);

            return DeliveryOutcome.Success;
        }
        catch (Exception exception)
        {
            await RollbackQuietlyAsync(transaction);

            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return DeliveryOutcome.Failed($"{handlerName}: {exception.GetType().Name}: {exception.Message}", exception);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static async Task InvokeAsync(Type handlerInterface, object handler, IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var handleAsync = handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        try
        {
            await (Task)handleAsync.Invoke(handler, [domainEvent, cancellationToken])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static async Task RollbackQuietlyAsync(ITransaction transaction)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // The original failure is the one worth reporting; disposal below discards the transaction anyway.
        }
    }

    private static string HandlerName(object handler)
    {
        var name = handler.GetType().FullName ?? handler.GetType().Name;

        return name.Length > OutboxHandlerReceipt.MaxHandlerLength ? name[..OutboxHandlerReceipt.MaxHandlerLength] : name;
    }
}
