namespace Raftel.Infrastructure.Data;

public interface IDataFilter
{
    bool IsEnabled<TFilter>();
    IDisposable Disable<TFilter>();
}