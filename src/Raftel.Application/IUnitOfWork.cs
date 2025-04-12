namespace Raftel.Application;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
