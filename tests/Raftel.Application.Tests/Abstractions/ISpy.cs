namespace Raftel.Application.Tests.Abstractions;

public interface ISpy
{
    void Intercept(string message);
    string[] InterceptedMessages();
}